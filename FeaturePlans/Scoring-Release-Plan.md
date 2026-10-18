# Scoring refactor: release plan

How the `new-scoring-system-db-compat` work goes to production with no downtime and a way
back at every step. The new club systems (PBG, VARC 2026, Wawasee 2026) are switched on
separately, after the code is live.

## Goals

- **No downtime.** The database changes go in first and work with the old code; the new
  code then arrives with a slot swap.
- **Rollback at every step.** Until the new club systems are in use, rolling back is
  swapping the slots back. The database stays migrated: the old code runs on it.
- **Existing results don't change.** Checked against a restored copy of production before
  anything reaches prod.

## What changes in the database

Six migrations, all additive: one widens two columns, the rest add nullable columns (or with
defaults) and one new table.
The old code ignores them, and its inserts get the defaults.

| Migration | Schema | Data |
|-----------|--------|------|
| `IncreaseFleetNameLengthTo40` | `Fleets.ShortName`, `.NickName` to 40 characters | Already on `main` but not yet applied to prod; `main`'s code already allows 40 |
| `AddScoringCalculatorType` | `ScoringSystems.CalculatorType` | Backfills base systems from their names |
| `AddScoringSystemSettingsAndDiscardMethod` | `ScoringSystems.DiscardMethod`, `.Settings` | |
| `AddScoreCodeIncludeInAverage` | `ScoreCodes.IncludeInAverage` | |
| `AddSummaryRollupModeAndScoringSystemClubAccess` | `Series.SummaryRollupMode`, `.IncludeUnrankedChildScores`; `ScoringSystems.IsRestrictedToClubs`; table `ScoringSystemClubAccess` | Copies `ChildrenSeriesAsSingleRace` into `SummaryRollupMode` |
| `InheritDiscardPatternAndParticipation` | | Child systems with a blank discard pattern or participation get the old meaning written in ("0", or 5 under Top X), because blank now means "inherit" |

`ChildrenSeriesAsSingleRace` stays: the new code keeps it in step with `SummaryRollupMode`,
so the old code reads the same mode. Drop it in a later migration, once rolling back to the
old code is no longer an option.

## Scripts

All in `Sql Utility Scripts/Scoring release/` unless noted.

| Script | When | What |
|--------|------|------|
| `deploy.sql` | Step 2 (local), step 5 (prod) | The six migrations, from `dotnet ef migrations script 20260909115239_AddScoreCodeGroups --idempotent`. Each migration in its own transaction, skipped if already applied, so it's safe to rerun. Regenerate it if a migration changes. |
| `pre-swap.sql` | Step 7, just **before** the swap | Repeats the data backfills for rows the old code added or changed since step 5. Must run while the old code is live: after the swap, a blank discard pattern can be deliberate. Set `@SmokeTestClubInitials` to leave the test club's systems alone. |
| `post-swap.sql` | Step 7, just **after** the swap | Repeats the one backfill that is safe with the new code live (summary rollup mode), and lists anything to review by hand. |
| `../Create PBG Scoring system.sql`, `../Create VARC 2026 Scoring systems.sql`, `../Create Wawasee 2026 Scoring system.sql` | Step 10 | Create the new base systems restricted to the one club in `@AccessClubInitials`, in one transaction. Open them to more clubs in Site Admin > Shared Scoring Systems. Safe to rerun. |

The production parity check is `SailScores.Test.Unit/Core/Scoring/Characterization/ProdParityTests.cs`.

## Steps

### 1. Restore prod locally

Restore the latest production backup to a local SQL Server database, for example
`SailScoresProdCopy`. Check its last migration:

```sql
SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
-- expect 20260909115239_AddScoreCodeGroups (one behind main)
```

If it's anything else, regenerate `deploy.sql` from prod's last migration.

### 2. Rehearse on the copy

1. Run `deploy.sql` on the copy. Run it a second time: it should change nothing.
2. **Production parity check.** Every series rescored by `main` and by the branch, then
   compared. Both runs score inside a transaction they roll back, so the copy is unchanged.
   Results contain real names: keep the output folder outside the repo.

   ```powershell
   $env:PROD_PARITY_CONNECTION = "Server=localhost;Database=SailScoresProdCopy;User Id=sa;password=...;TrustServerCertificate=True;"
   $env:PROD_PARITY_DIR = "C:\prod-parity"
   $env:PROD_PARITY_PARALLEL = "4"   # optional

   # Baseline with main (C:\temp\main is a worktree of main; any will do)
   New-Item -ItemType Directory -Force C:\temp\main\SailScores.Test.Unit\Core\Scoring\Characterization | Out-Null
   Copy-Item SailScores.Test.Unit\Core\Scoring\Characterization\ProdParityTests.cs C:\temp\main\SailScores.Test.Unit\Core\Scoring\Characterization\
   dotnet build C:\temp\main\SailScores.Test.Unit\SailScores.Test.Unit.csproj -p:TypeScriptCompileBlocked=true
   C:\temp\main\SailScores.Test.Unit\bin\Debug\net10.0\SailScores.Test.Unit.exe -class SailScores.Test.Unit.Core.Scoring.Characterization.ProdParityTests

   # Compare with the branch
   dotnet test --project SailScores.Test.Unit/SailScores.Test.Unit.csproj --filter-class "*ProdParityTests"
   ```

   `main` runs its test executable directly: its `dotnet test` uses VSTest, which the .NET
   10 SDK refuses for this project. Don't commit the copied file on `main`.

   The baseline run is also the rollback rehearsal: it is the old code on the migrated
   database. Any failure in it (an `ERROR` line in a baseline file that the branch doesn't
   have) is a rollback problem.

   A difference is listed in `C:\prod-parity\changed.txt`, with a `.received.txt` beside
   its baseline. Expected: none, because no existing calculator's scoring changed. Investigate
   every one before going further.
3. **Rollback rehearsal with the app.** Run the branch locally against the copy, recalculate
   a few series (one Appendix A, one summary series, one handicap), then run `main` against
   the same copy and open those series' pages. The old code must read results the new code
   saved.

### 3. Squash for the public repo

Squash onto the latest public `main` in a new branch. The history before `5d830271` has
real competitor names in the fixtures, so it must not go to the public repo.

- `git diff new-scoring-system-db-compat <squashed>` shows only what `main` changed since.
- Rerun on the squashed commit: unit tests, the corpus check, and the production parity
  check (baseline from step 2 is fine). Stage gets exactly this commit.

### 4. Back up prod

Confirm the point-in-time restore window, or take a copy-only backup. A last resort: the
plan never needs a database restore to roll back.

### 5. Migrate prod

Run `deploy.sql` on prod. The old code keeps running on it. From here to step 7, keep the
time short (hours, not days): the old code can create data the backfills haven't seen,
which `pre-swap.sql` catches up.

**Rollback:** none needed; the old code runs on the migrated database.

### 6. Deploy to stage and smoke test

Deploy the squashed commit to the stage slot (which uses the prod database).

Stage writes to prod, so:
- Use the test club for anything that saves: recalculating, editing a scoring system,
  What-If.
- On real clubs, only view pages.
- Stage runs its own job queue (`QueuedHostedService`) against prod.

Smoke tests (test club unless noted):
- Home page, a club page and series results for a few real clubs load (view only).
- Recalculate a test-club series with each base system type: Appendix A, a low point
  average, high point percentage, Cox-Sprague, Top X, a handicap series, a summary series.
  Compare with the results before recalculating: only the new columns and headings differ.
- Scoring system edit pages: create and edit a child system (inherited placeholders,
  special fields panel).
- HTML export and What-If.
- Login works, and still works after the swap (data protection keys are in Azure blob
  storage; both slots must use the same key store).

**Rollback:** redeploy the previous build to stage. Prod is untouched.

### 7. Swap

1. Run `pre-swap.sql` on prod, with `@SmokeTestClubInitials` set to the test club. Note the
   counts it prints.
2. Swap stage and prod.
3. Run `post-swap.sql`. Review any rows it lists.

**Rollback:** swap back. The database stays as it is. Results the new code saved in the
meantime are read by the old code (rehearsed in step 2). Club systems saved by the new code
with a blank discard pattern would mean "none" again to the old code; `post-swap.sql`'s
review list shows them.

### 8. Watch

Watch Application Insights for errors, and the club pages most used in the next few days.
Keep the previous build in the stage slot until you're confident, so swapping back stays
one click.

### 9. Before switching on the new club systems

Finish section F of the manual test checklist: compare each club system with the club's
Sailing Instructions, check every score code (VARC's codes copied from Top X include fixed
0s), and settle the Wawasee average rounding. Record the rules in `docs/ScoringSystems/`.
Fixes here are free while no club has saved results with these systems.

### 10. New club systems, one club at a time

1. Run each setup script with `@AccessClubInitials` set to the test club. The systems are
   created restricted: only that club sees them.
2. Score test series with them on the test club.
3. When a club's scorekeepers are ready, add their club in Site Admin > Shared Scoring
   Systems and work through their series with them.

**Rollback:** remove the club from the system's access list. Once a club has saved
results with a system, changes to how it scores change those results: treat it like any
existing calculator.

## Later

- Drop `Series.ChildrenSeriesAsSingleRace` in its own migration, once rolling back to the
  old code is no longer needed.
- The follow-up work in `FeaturePlans/Scoring-Calculator-Refactor-Plan.md`.

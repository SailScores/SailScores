# Scoring characterization tests

These tests score real series from club backups and compare the results with approved
snapshots. They guard the golden rule in `docs/Scoring-AddingACalculator.md`: a refactor
or new feature must not change how an existing calculator scores.

| File | Purpose |
|---|---|
| `../Fixtures/*.json[.gz]` | Series cut from club backups by `SailScores.FixtureTool` |
| `../Fixtures/shared-systems.json` | The shared base scoring systems, exported by the tool |
| `ScoringFixture` | Loads a fixture; can swap in a shared base system |
| `FixtureScorer` | Restores a fixture into an in-memory database and scores it through `SeriesService` |
| `ResultsSnapshot` | Renders results as stable, readable JSON (no ids or timestamps) |
| `Snapshot` | Compares with `Snapshots/<name>.verified.json` |
| `ScoringFixtureTests` | One snapshot per fixture, plus three fixtures scored with every shared base system (`Snapshots/SharedSystems/`) |
| `ScoringInvariantTests` | Rules any correct result follows, checked over every case |
| `CorpusParityTests` | Opt-in: scores every fixture in `FIXTURE_CORPUS_DIR` against a local baseline |

## When a snapshot test fails

The test writes `<name>.received.json` next to the `.verified.json` file. Diff the two.

- **Unintended change:** fix the code.
- **Intended change** (a new feature with its default off doesn't count; existing output
  must not move): get the maintainer's approval, then replace `.verified.json` with the
  received file, or rerun with `SAILSCORES_ACCEPT_SNAPSHOTS=1`. Review the diff before
  committing.

`*.received.json` is gitignored.

## Reading a snapshot

Each competitor's race scores are one string, in race order: `3` (place 3, 3 points),
`2:2.5` (place 2, 2.5 points), `DNC 12` (code, points), `(DNC 12)` (discarded), `7/9`
(percentage systems: 7 of a possible 9), `-` (no score in that race).

## Adding a fixture

1. Take a club backup (Club Admin > Backup) and export the shared systems:
   `dotnet run --project SailScores.FixtureTool -- shared-systems --connection "<conn>" --out shared-systems.json`
2. Find series that exercise what you need:
   `dotnet run --project SailScores.FixtureTool -- list <backup> --shared shared-systems.json --out series.csv`
3. Extract them into `../Fixtures` (add `--gzip` for big ones):
   `dotnet run --project SailScores.FixtureTool -- extract <backup> --shared shared-systems.json --out SailScores.Test.Unit/Core/Scoring/Fixtures --series <id>`
4. Run the tests with `SAILSCORES_ACCEPT_SNAPSHOTS=1` to write the new snapshot, and
   check it against the club's published results.

For a new calculator, add its base system to `shared-systems.json` and the three variant
fixtures are scored with it automatically. Run its script on local dev, then update just
that system, which keeps the others even if your database doesn't have them:
`dotnet run --project SailScores.FixtureTool -- shared-systems --connection "<conn>" --out SailScores.Test.Unit/Core/Scoring/Fixtures/shared-systems.json --only <system id>`.
If it needs settings that a club's child system would set, add them to
`ScoringFixture.VariantSettings`. A full export (without `--only`) includes the
club-restricted systems (PBG, VARC, Wawasee), so run their scripts in `Sql Utility Scripts`
on the database first.

## Corpus parity (local only)

The committed fixtures are a sample. To check a change against every series you have
extracted, keep the full extract in a folder outside the repo and set `FIXTURE_CORPUS_DIR`:

```
$env:FIXTURE_CORPUS_DIR = "D:\scoring-corpus"
dotnet test --project SailScores.Test.Unit/SailScores.Test.Unit.csproj --filter-class "*CorpusParityTests"
```

The first run writes `<corpus>/.baseline/`. Later runs fail listing each series whose results
changed, with a `.received.json` beside its baseline. Make the baseline on a commit you trust
(for example main) before starting a change. Delete `.baseline` after an approved behaviour
change.

## Coverage

```
dotnet test --project SailScores.Test.Unit/SailScores.Test.Unit.csproj --coverage --coverage-output-format cobertura --coverage-output coverage.xml
```

## Current fixtures

Chosen to cover, between them, both Appendix A calculators the clubs use, every score code
found in three clubs' backups, ties, a summary series, a multi-fleet series, participation
rules and every club scoring system in those backups. Taken from backups made before score
code groups and season default scoring systems were included.


# Handicap Race Scoring — Feature Plan

## Executive Summary

This is a significant architectural addition. Handicap scoring differs fundamentally from the existing
place-based scoring: finish *times* are recorded per boat, a handicap formula converts elapsed time to a
*corrected time*, boats are then ranked by corrected time, and only then do those ranks feed the normal
low-point/series scoring pipeline. The plan below covers data model, scoring pipeline, UI, and open
questions.

This feature must be implemented so that clubs using one-design scoring see no degradation in UI
usability — handicap-specific fields and controls appear only when a series is configured for handicap
scoring.

---

## 1. Handicap Systems to Support

### Tier 1 (MVP)

| System | Formula | Notes |
|--------|---------|-------|
| **PHRF Time-on-Distance (ToD)** | `corrected = elapsed_sec - (rating × distance_nm)` | Most common in North America for keelboats. Rating = seconds/mile advantage over scratch boat (rating 0). |
| **PHRF Time-on-Time (ToT)** | `corrected = elapsed_sec × 600 / (600 + rating)` | Same PHRF rating, no distance required; preferred when courses vary in length. |
| **Portsmouth Yardstick (PY)** | `corrected = elapsed_sec / PY × 1000` | Standard for UK club dinghy racing; ratings published annually by RYA (baseline = 1000). |

### Tier 2 (Later)

- **IRC / ORC** — requires a Time Correction Coefficient (TCC) certificate with an expiry date. The
  TCC could be modelled as a `HandicapValue` in a ToT-style formula
  (`corrected = elapsed_sec × TCC`) once Tier 1 is solid, with `EffectiveTo` on
  `CompetitorHandicap` enforcing certificate expiry.
- **Custom club systems** — clubs define their own rating table and formula via a configurable
  multiplier on the `Custom` system type.

### Out of scope

- **Pursuit racing** — boats start at staggered times based on handicap; first to finish wins. A
  fundamentally different format, deferred to a future phase.

---

## 2. Data Model

### 2a. New: `HandicapSystem` entity

```
HandicapSystem
  Id          Guid
  ClubId      Guid?   (null = site-wide standard; never modified by clubs)
  Name        string  (e.g., "PHRF Time-on-Distance", "Portsmouth")
  SystemType  enum    (PhrfToD | PhrfToT | Portsmouth | TimeOnTime | Custom)
  Description string?
```

- Site-wide systems are seeded like `ScoringSystem` shared systems (ClubId = null).
- A club can create club-specific variants with ClubId set.
- IRC/ORC support could be added as a `TimeOnTime` system type variant with certificate-expiry
  semantics on `CompetitorHandicap.EffectiveTo`.

### 2b. Handicap system hierarchy

Handicap system selection cascades from most-specific to least-specific:

```
Series.HandicapSystemId  (overrides fleet)
  → Fleet.HandicapSystemId  (overrides club)
    → Club.HandicapSystemId  (club default)
```

The scoring calculator resolves the effective system by walking this chain. If no system is found at
any level, the series is treated as one-design (no time correction).

### 2c. New: `CompetitorHandicap` entity

Stores the rating for a competitor under a particular handicap system, with effective date range to
handle mid-season adjustments.

```
CompetitorHandicap
  Id                Guid
  CompetitorId      Guid   → Competitor
  HandicapSystemId  Guid   → HandicapSystem
  Value             decimal  (interpretation depends on system type)
  EffectiveFrom     DateTime?
  EffectiveTo       DateTime?
  Notes             string?
```

**Effective date rules (enforced by filtered unique indexes):**

- At most one row per `(CompetitorId, HandicapSystemId)` may have `EffectiveFrom IS NULL`.
- At most one row per `(CompetitorId, HandicapSystemId)` may have `EffectiveTo IS NULL`.
- A single row may have both nulls (competitor has exactly one rating, valid forever).
- Once a competitor has more than one rating entry for a system, dates are required to avoid ambiguity.

**Lookup rule:** for a race with a given date, find the row where
`(EffectiveFrom IS NULL OR EffectiveFrom <= race.Date)` AND
`(EffectiveTo IS NULL OR EffectiveTo >= race.Date)`.

**Alternative considered:** putting the handicap directly on `Competitor`. Rejected because: (a)
competitors race in multiple systems, (b) values change over a season, (c) clubs run multiple handicap
systems simultaneously.

### 2d. `Score` — add corrected-time fields

```
Score
  + CorrectedTime   TimeSpan?   (computed at series-calculation time; stored for display)
  + HandicapValue   decimal?    (snapshot of the rating applied, for audit and display)
```

`HandicapValue` may be manually overridden at race-entry time (e.g., a guest boat given a trial
rating for one event). When set manually, Phase 0 uses this value instead of looking up
`CompetitorHandicap`.

### 2e. `Race` — add `CourseDistance`

```
Race
  + CourseDistance  decimal?   (nautical miles; required for PHRF ToD, optional otherwise)
```

### 2f. `Series` — link to handicap system

```
Series
  + HandicapSystemId  Guid?   (if resolved system is non-null, series uses time-correction scoring)
```

A series is either fully handicap or fully one-design — mixed scoring within a single series is not
supported. Clubs wanting mixed fleets in one event should use separate series combined via a Summary
series.

### 2g. `Fleet` and `Club` — default handicap system

```
Fleet
  + HandicapSystemId  Guid?

Club
  + HandicapSystemId  Guid?
```

Both nullable; absence means "inherit from parent or treat as one-design."

---

## 3. Scoring Pipeline Changes

The existing pipeline is: `Score.Place` (raw finish order) → calculator → series standings.

Handicap adds a pre-processing phase:

```
Phase 0 (new): Time Correction
  Resolve HandicapSystem for the series (Series → Fleet → Club cascade)
  For each race in the series:
    For each Score with ElapsedTime set:
      Look up CompetitorHandicap using race.Date and effective-date rules
        (or use Score.HandicapValue if manually overridden)
      Compute CorrectedTime using the system formula
      Store CorrectedTime and HandicapValue on Score
    Rank competitors by CorrectedTime ascending → assign Place
    Competitors with no elapsed time and no code → assign NHC code (see §6)

Phase 1 (existing): Series scoring
  BaseScoringCalculator uses Score.Place as normal
```

Phase 0 runs at series calculation time (not at race entry), so retroactive handicap corrections
automatically flow into recalculated results. The `Score.HandicapValue` snapshot preserves what was
applied for audit purposes.

### New calculator classes

```
HandicapScoringCalculator (abstract, extends BaseScoringCalculator)
  abstract TimeSpan ComputeCorrectedTime(elapsed, handicapValue, distance?)
  override SetScores → runs Phase 0 first, then calls base

PhrfToDCalculator    : HandicapScoringCalculator
PhrfToTCalculator    : HandicapScoringCalculator
PortsmouthCalculator : HandicapScoringCalculator
```

Registered in `ScoringCalculatorFactory` keyed by `HandicapSystem.SystemType`.

Note: IRC/ORC could be added as `IrcCalculator : HandicapScoringCalculator` using the same ToT
formula with `Value = TCC`; the only additional concern is certificate expiry, which `EffectiveTo`
already handles.

---

## 4. Multi-System and Multi-Fleet Clubs

A club running both PHRF keelboats and Portsmouth dinghies has:

- A "Keelboats" fleet with `HandicapSystemId` → PHRF ToD
- A "Dinghies" fleet with `HandicapSystemId` → Portsmouth
- Each fleet's series inherits the appropriate system automatically

`CompetitorHandicap` is keyed by `(CompetitorId, HandicapSystemId)`, so a competitor racing in both
fleets can hold a PHRF rating and a PY number simultaneously.

---

## 5. Competitor Handicaps in Different Events

**a) Mid-season adjustment** — PHRF committee adjusts a boat's rating. Add a new
`CompetitorHandicap` row with an `EffectiveFrom` date; the old row gets an `EffectiveTo` date. Past
races resolve to the old rating; future races resolve to the new one. The `Score.HandicapValue`
snapshot shows what was applied at calculation time.

**b) Different system per event** — A boat races PHRF at its home club but IRC at a regatta.
`HandicapSystemId` distinguishes the two ratings; both rows coexist on `CompetitorHandicap`.

**c) One-off trial rating** — A guest boat is given a provisional rating for one event. The scorer
enters a `HandicapValue` override directly on the score at race-entry time; Phase 0 uses this
instead of the `CompetitorHandicap` lookup.

---

## 6. Missing Handicap — NHC Code

If a competitor in a handicap race has no resolved handicap (no `CompetitorHandicap` row and no
manual override) and no finish code, the system assigns the **NHC** (No Handicap Certificate) code
rather than blocking the save. This:

- Allows the race to be saved and the series to be scored without interruption.
- Sorts NHC to the bottom of results, equivalent to DNC points.
- For users with admin permissions, the NHC result in the race detail view links directly to the
  competitor's handicap management page so the problem can be fixed immediately.

NHC is added as a standard `ScoreCode` entry (configurable points, defaulting to DNC-equivalent)
in the site-wide scoring system seed data.

---

## 7. Results Display

**Race detail page** — for handicap races, show:

| Pos | Competitor | Elapsed Time | Handicap | Corrected Time |
|-----|-----------|-------------|---------|---------------|

**Series standings** — show place only, identical in structure to one-design standings. Elapsed and
corrected times are one click away via race detail links that already exist in the UI.

This keeps the series standings presentation consistent between one-design and handicap series,
satisfying the usability constraint.

---

## 8. Scratch Boat Convention

PHRF rating 0 = scratch boat (no time correction). Portsmouth 1000 = baseline. These are documented
in the help/FAQ; no special UI treatment is needed.

---

## 9. Handicap Import

PHRF is administered by a dozen regional organizations in North America with no unified API. RYA
Portsmouth numbers are published as a static annual table. Manual entry covers the vast majority of
cases. A CSV import path can be added in a future phase.

---

## 10. Migration & Rollout Plan

1. **Schema** — add `HandicapSystem`, `CompetitorHandicap` tables; add nullable `HandicapSystemId`
   to `Club`, `Fleet`, `Series`; add `CourseDistance` to `Race`; add `CorrectedTime` and
   `HandicapValue` to `Score`. All additive; no breaking changes to existing clubs.
2. **Seed data** — insert site-wide `HandicapSystem` rows for PHRF ToD, PHRF ToT, Portsmouth. Add
   NHC `ScoreCode` to the site-wide scoring system.
3. **Filtered unique indexes** — enforce null-effective-date constraints on `CompetitorHandicap`.
4. **Core scoring** — implement `HandicapScoringCalculator` family; register in factory.
5. **Race entry UI** — show elapsed-time and course-distance fields only when the series resolves a
   handicap system; hidden for one-design series.
6. **Results display** — update race detail view for corrected-time columns; series standings
   unchanged.
7. **Competitor admin** — add handicap rating management section to competitor edit page.
8. **Club/Fleet/Series admin** — add handicap system dropdown to each, with clear "inherit from
   parent" default option.
9. **Tests** — pure unit tests for each formula; integration tests for corrected-time →
   series-standings pipeline; verify one-design series are unaffected.

---

## 11. Key Architectural Risks

- **Elapsed time data quality** — `Score.ElapsedTime` and `Race.StartTime` exist but are not widely
  used today. Verify consistent population before depending on them for handicap calculations.
- **Calculator factory coupling** — `ScoringCalculatorFactory` currently keys on `ScoringSystem`;
  handicap calculators also need the resolved `HandicapSystem`. The factory signature or constructor
  injection may need to change.
- **Performance** — look up all `CompetitorHandicap` rows for a race in a single batch query at
  calculator construction time rather than per-score to avoid N+1 queries on large fleets.
- **One-design UX regression** — every UI change must be gated on whether the series resolves a
  handicap system. Add integration tests that assert one-design series entry and display flows are
  unchanged.

# Club scoring systems: rules and sources

Club scoring systems (Wawasee 2026, VARC 2026, PBG, …) implement a club's own rules, usually
from its Sailing Instructions (SIs). This folder keeps those rules next to the code: the
club's document, and a page per system showing where each rule is implemented and tested.

## Systems

| System | Club | Page | Recipe | Tests |
|--------|------|------|--------|-------|
| Wawasee 2026 | Wawasee | [Wawasee2026.md](Wawasee2026.md) | `Scoring/Systems/Wawasee2026.cs` | `Wawasee2026CalculatorTests` |
| VARC 2026, VARC 2026 Shorthanded | VARC | [Varc2026.md](Varc2026.md) | `Scoring/Systems/TopX.cs` (`Varc2026`) | `Varc2026CalculatorTests` |
| PBG | PBGMYS | [Pbg.md](Pbg.md) | `Scoring/Systems/LowPointAverages.cs` (`Pbg`) | `PbgCalculatorTests` |
| Appendix B Windsurfing | All clubs (shared base system, RRS 2025-2028) | [AppendixB-Windsurfing.md](AppendixB-Windsurfing.md) | `Scoring/Systems/AppendixB.cs` (`Windsurfing`) | `AppendixBWindsurfingCalculatorTests` |

Recipe paths are under `SailScores.Core/`; tests are under `SailScores.Test.Unit/Core/Scoring/`.

## Layout

```
docs/ScoringSystems/
  README.md            this file: guidelines and the index above
  Wawasee2026.md       one page per system (or per family, e.g. both VARC 2026 systems)
  sources/
    Wawasee-2026-SIs.pdf   the club's document, as received
```

## What a system page holds

1. **Source.** The club, the document's title and date or version, who provided it, and a
   link if it's online. Point to the file in `sources/`.
2. **The scoring rules, quoted.** Only the scoring sections, not the whole SIs.
3. **A clause-by-clause map.** Each rule → the recipe part or rule class that implements it →
   the test that pins it. For example:

   | Rule (SIs) | Implemented by | Pinned by |
   |------------|----------------|-----------|
   | DNC scores the average of finishes in races that count | `Adjustments/AverageFinishOverCountedRaces` | `Wawasee2026CalculatorTests.CalculateResults_MissedRaces_ScoreAverageFinishAndPenaltyBelowThreshold` |

   A review against the SIs is then a matter of going down the list.
4. **Settings and score codes.** Which values come from the SIs (thresholds, participation
   percent, score code group limits), and the points each score code should give. The score
   codes are data, created by the scripts in `Sql Utility Scripts/`, so check them too.
5. **Open questions.** Anything the SIs leave unclear or the code doesn't yet follow, until
   it's resolved. Mark a rule "unconfirmed" until it has been checked against the club's
   document.

## Versions

SIs change from year to year, which is why club systems are named by year. When a club's
rules change, add a new system (for example Wawasee 2027) with its own page and source file.
Keep the old page: it records what the old calculator implements, and saved results scored
by it are recalculated with it. Don't edit an existing system to follow new rules; see
[Scoring-AddingACalculator.md](../Scoring-AddingACalculator.md).

The exception is a system that doesn't yet follow the rules it was written for. Fixing that
changes saved results when they're recalculated, so tell the club first, and record the
change in the page's open questions and in [Scoring-Quirks.md](../Scoring-Quirks.md).

## Code links

The recipe's header comment should name its page here, so anyone changing the recipe can
find the source rules. A new club system needs a page before it's merged.

## Before committing a club's document

- Ask the club whether they're happy for it to be in the repository, which may be public.
- Remove contact details (phone numbers, email addresses) and anything else personal.
- If the full SIs are long, keep only the scoring sections, as Markdown or an excerpt PDF.

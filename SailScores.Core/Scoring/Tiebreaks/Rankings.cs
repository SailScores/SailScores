namespace SailScores.Core.Scoring.Tiebreaks;

// The rankings recipes share. A system with its own rules builds a Ranking in its recipe
// (see Systems/Wawasee2026.cs, Systems/TopX.cs).
public static class Rankings
{
    // Appendix A family and low point averages: lower total, then RRS A8.1, then A8.2.
    public static readonly Ranking LowPoint =
        Ranking.By(new LowestTotal(zeroTotalRanksLast: true))
            .Then(new AppendixA81LowPoint())
            .Then(AppendixA82.LowPoint);

    // High point percentage, Cox-Sprague and Top X: higher total, then more races sailed,
    // then RRS A8.1, then A8.2.
    public static readonly Ranking HighPoint =
        Ranking.By(new HighestTotal())
            .Then(new MoreRacesSailed())
            .Then(new AppendixA81HighPoint())
            .Then(AppendixA82.HighPoint);

    // PWA Standard: lower total, then the PWA tiebreaks (A), (B) and (C), then the last race
    // (E). (D), a sail-off, can't be scored.
    public static readonly Ranking Pwa =
        Ranking.By(new LowestTotal(zeroTotalRanksLast: true))
            .Then(new PwaBeatenMoreOften())
            .Then(new PwaBetterScoresIncludingDiscards())
            .Then(new PwaBetterDiscard())
            .Then(AppendixA82.LowPoint);
}

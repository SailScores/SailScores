using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Enumerations
{
    // Selects the scoring calculator for a base scoring system (one with no parent).
    // Stored as an int: do not renumber existing values.
    public enum ScoringCalculatorType
    {
        [Display(Name = "Appendix A")]
        AppendixA = 1,
        [Display(Name = "Appendix A (pre-2025)")]
        AppendixAPre2025 = 2,
        [Display(Name = "Appendix A, First = 0")]
        AppAAltFirstIsZero = 3,
        [Display(Name = "Appendix A, First = .7")]
        AppAAltFirstIsPoint7 = 4,
        [Display(Name = "Appendix A, First = .75")]
        AppAAltFirstIsPoint75 = 5,
        [Display(Name = "Low Point Average")]
        LowPointAverageInclDnc = 6,
        [Display(Name = "Low Point Average without DNCs")]
        LowPointAverageExclDnc = 7,
        [Display(Name = "High Point Percentage")]
        HighPointPercentage = 8,
        [Display(Name = "Cox-Sprague")]
        CoxSprague = 9,
        [Display(Name = "Top X High Point")]
        TopXHighPoint = 10,
        [Display(Name = "PWA Standard")]
        PwaStandard = 11,
        // 12 and 13 were early PBG systems, removed before release. Don't reuse them.
        [Display(Name = "PBG")]
        Pbg = 14,
        [Display(Name = "VARC 2026 Shorthanded")]
        Varc2026Shorthanded = 15,
        [Display(Name = "VARC 2026")]
        Varc2026 = 16,
        [Display(Name = "Wawasee 2026")]
        Wawasee2026 = 17,
        [Display(Name = "Appendix B Windsurfing")]
        AppendixBWindsurfing = 18
    }
}

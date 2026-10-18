using SailScores.Api.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{
    public class ScoringSystem
    {
        public Guid Id { get; set; }
        public Guid? ClubId { get; set; }

        [Required]
        [StringLength(100)]
        public String Name { get; set; }

        // This system's own pattern; null or blank inherits from the parent.
        [DisplayName("Discard Pattern")]
        public String DiscardPattern { get; set; }

        // Pattern resolved along the parent chain by ScoringService (nearest non-blank).
        // Falls back to this system's own value when it hasn't been resolved, so systems
        // built directly (e.g. in tests) still work. Calculators should read this.
        public String EffectiveDiscardPattern
        {
            get => _effectiveDiscardPattern ?? DiscardPattern;
            set => _effectiveDiscardPattern = value;
        }
        private String _effectiveDiscardPattern;

        public IList<ScoreCode> ScoreCodes { get; set; }

        public Guid? ParentSystemId { get; set; }

        public ScoringSystem ParentSystem { get; set; }

        public IEnumerable<ScoreCode> InheritedScoreCodes { get; set; }

        public IList<ScoreCodeGroup> ScoreCodeGroups { get; set; }

        public IEnumerable<ScoreCodeGroup> InheritedScoreCodeGroups { get; set; }

        // This system's own value; null inherits from the parent.
        // (Top X High Point uses it as the number of races to count.)
        [DisplayName("Participation Percent")]
        public Decimal? ParticipationPercent { get; set; }

        // Resolved along the parent chain like EffectiveDiscardPattern, with the same
        // fallback. Calculators should read this.
        public Decimal? EffectiveParticipationPercent
        {
            get => _effectiveParticipationPercent ?? ParticipationPercent;
            set => _effectiveParticipationPercent = value;
        }
        private Decimal? _effectiveParticipationPercent;

        // Calculator for this system, resolved from the root of the parent chain
        // by ScoringService. Never written back to child systems.
        public ScoringCalculatorType? CalculatorType { get; set; }

        // This system's own settings; null values inherit from the parent.
        public ScoringSystemSettings Settings { get; set; } = new ScoringSystemSettings();

        // Settings merged along the parent chain, resolved by ScoringService.
        // Calculators should read these, not Settings.
        public ScoringSystemSettings EffectiveSettings { get; set; } = new ScoringSystemSettings();

        // This system's own discard method; null inherits from the parent.
        [DisplayName("Discard Method")]
        public DiscardMethod? DiscardMethod { get; set; }

        // Discard method resolved along the parent chain by ScoringService.
        public DiscardMethod EffectiveDiscardMethod { get; set; } = Api.Enumerations.DiscardMethod.RaceCountPattern;

    }
}

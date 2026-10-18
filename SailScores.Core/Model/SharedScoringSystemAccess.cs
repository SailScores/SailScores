using System;
using System.Collections.Generic;
using SailScores.Api.Enumerations;

namespace SailScores.Core.Model
{
#pragma warning disable CA2227 // Collection properties should be read only
    // Which clubs can use a shared (ClubId == null) scoring system as a parent.
    public class SharedScoringSystemAccess
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? ParentSystemId { get; set; }
        public string ParentSystemName { get; set; }
        public ScoringCalculatorType? CalculatorType { get; set; }

        // New clubs get child systems of these, so they can't be restricted.
        public bool IsUsedForNewClubs { get; set; }

        public bool IsRestrictedToClubs { get; set; }

        // Clubs with access when IsRestrictedToClubs is set.
        public IList<Guid> ClubIds { get; set; } = new List<Guid>();
    }
#pragma warning restore CA2227 // Collection properties should be read only
}

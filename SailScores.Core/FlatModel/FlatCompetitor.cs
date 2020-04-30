using System;

namespace SailScores.Core.FlatModel
{
    public class FlatCompetitor
    {
        public Guid Id { get; set; }
        public String Name { get; set; }
        public String SailNumber { get; set; }
        public String AlternativeSailNumber { get; set; }
        public String BoatName { get; set; }
        public String HomeClubName { get; set; }

        public String CurrentSailNumber { get; set; }

    }
}
using System;

namespace Sailscores.Database.Entities
{
    public class CompetitorFleet
    {
        public Guid CompetitorId { get; set; }
        public Competitor Competitor { get; set; }

        public Guid FleetId { get; set; }
        public Fleet Fleet { get; set; }
    }
}

using System;

namespace Sailscores.Database.Entities
{
    public class FleetBoatClass
    {
        public Guid FleetId { get; set; }
        public Fleet Fleet { get; set; }

        public Guid BoatClassId { get; set; }
        public BoatClass BoatClass { get; set; }
    }
}

using System;
namespace SailScores.Database.Entities
{
    public class SeriesRace
    {
        public Guid RaceId { get; set; }
        public Race Race { get; set; }

        public Guid SeriesId { get; set; }
        public Series Series { get; set; }
    }
}

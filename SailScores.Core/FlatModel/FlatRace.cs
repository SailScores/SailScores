using SailScores.Api.Enumerations;
using System;

namespace SailScores.Core.FlatModel
{
    public class FlatRace
    {
        public Guid Id { get; set; }

        public String Name { get; set; }
        public DateTime? Date { get; set; }
        public int Order { get; set; }
        public String Description { get; set; }

        public RaceState? State { get; set; }

    }
}
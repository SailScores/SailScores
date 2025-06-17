using SailScores.Api.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{
    public class SeriesAsRace : Race
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalChildRaceCount { get; set; } = 0;

    }
}

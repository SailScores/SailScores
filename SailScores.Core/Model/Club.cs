﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{

#pragma warning disable CA2227 // Collection properties should be read only
    public class Club
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        public String Name { get; set; }
        [StringLength(10)]
        public String Initials { get; set; }
        public String Description { get; set; }
        public bool IsHidden { get; set; }
        public bool? ShowClubInResults { get; set; }
        public String Url { get; set; }
        public bool? UseAdvancedFeatures { get; set; }

        public int? DefaultRaceDateOffset { get; set; }

        public WeatherSettings WeatherSettings { get; set; }

        public String Locale { get; set; }

        public IList<Fleet> Fleets { get; set; }
        public IList<Competitor> Competitors { get; set; }
        public IList<BoatClass> BoatClasses { get; set; }
        public IList<Season> Seasons { get; set; }
        public IList<Series> Series { get; set; }
        public IList<Race> Races { get; set; }

        public IList<Regatta> Regattas { get; set; }

        public ScoringSystem DefaultScoringSystem { get; set; }
        public Guid? DefaultScoringSystemId { get; set; }

        public IList<ScoringSystem> ScoringSystems { get; set; }

        public String StatisticsDescription { get; set; }

    }

#pragma warning restore CA2227 // Collection properties should be read only
}

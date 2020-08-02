using SailScores.Api.Enumerations;
using SailScores.Core.Extensions;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;

namespace SailScores.Web.Models.SailScores
{

#pragma warning disable CA2227 // Collection properties should be read only
    public class RaceSummaryViewModel
    {
        public Guid Id { get; set; }

        [StringLength(200)]
        public String Name { get; set; }

        [DisplayFormat(DataFormatString = "{0:D}")]
        public DateTime? Date { get; set; }

        // Typically the order of the race for a given date, but may not be.
        // used for display order after date. 
        public int Order { get; set; }
        [StringLength(1000)]
        public String Description { get; set; }

        public String FleetName { get; set; }
        public String FleetShortName { get; set; }

        public IList<KeyValuePair<String, String>> SeriesUrlAndNames { get; set; }

        public Season Season { get; set; }

        public IList<ScoreViewModel> Scores { get; set; }

        public RaceState? State { get; set; }

        public WeatherViewModel Weather { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public String CalculatedName
        {
            get
            {
                var raceWord = "Race";
                if (Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "fi")
                {
                    raceWord = "Purjehdukset";
                }
                if (Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "sv")
                {
                    raceWord = "Seglingar";
                }
                return $"{Date.ToShortString()} {raceWord} {Order}";
            }
        }
        public string ShortName
        {
            get
            {
                var raceLetter = "R";
                if (Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "fi")
                {
                    raceLetter = "k";
                }
                if (Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "sv")
                {
                    raceLetter = "S";
                }
                return $"{Date.ToSuperShortString()} {raceLetter}{Order}";
            }
        }

        public int CompetitorCount
        {
            get
            {
                if (Scores == null)
                {
                    return 0;
                }
                return Scores
                    .Count(s => s.ScoreCode?.CameToStart ?? 
                                (s.Place.HasValue && s.Place != 0));
            }
        }
    }
#pragma warning restore CA2227 // Collection properties should be read only
}

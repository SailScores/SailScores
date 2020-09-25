using SailScores.Api.Enumerations;
using System;
using System.Threading;
using SailScores.Core.Extensions;

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

        public DateTime? UpdatedDate { get; set; }
        public String UpdatedBy { get; set; }

        public Decimal? WindSpeedMeterPerSecond { get; set; }
        public Decimal? WindGustMeterPerSecond { get; set; }
        public Decimal? WindDirectionDegrees { get; set; }
        public String WeatherIcon { get; set; }


        public string WindSpeed { get; internal set; }
        public string WindGust { get; internal set; }
        public string WindSpeedUnits { get; internal set; }


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

    }
}
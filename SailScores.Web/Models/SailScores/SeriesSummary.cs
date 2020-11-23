using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Web.Models.SailScores
{
    public class SeriesSummary
    {

        public Guid Id { get; set; }
        public String Name { get; set; }
#pragma warning disable CA1056 // Uri properties should not be strings
        public String UrlName { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings
        public String Description { get; set; }
        public IList<Race> Races { get; set; }
        public Season Season { get; set; }

        public bool? IsImportantSeries { get; set; }

        public bool? ResultsLocked { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public String UpdatedBy { get; set; }

        public String DateString
        {
            get
            {
                if (Races?.Count == 0)
                {
                    return "No Races";
                }
                else if (Races?.Count == 1)
                {
                    return Races[0]?.Date?.ToString("M") ?? String.Empty;
                }

                var maxDate = Races.Max(r => r.Date);
                var minDate = Races.Min(r => r.Date);
                // if one has a value, they should both have values, but for completeness sake:
                if (maxDate.HasValue && minDate.HasValue)
                {
                    if (maxDate?.Date != minDate?.Date)
                    {
                        return minDate?.ToString("M") + " - " + maxDate?.ToString("M");
                    }
                    else
                    {
                        return minDate?.ToString("M");
                    }
                }
                // no max or min date
                return String.Empty;
            }
        }
    }
}

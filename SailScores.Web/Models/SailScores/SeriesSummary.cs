using Sailscores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sailscores.Web.Models.Sailscores
{
    public class SeriesSummary
    {

        public Guid Id { get; set; }
        public String Name { get; set; }
        public String Description { get; set; }
        public IList<Race> Races { get; set; }
        public Season Season { get; set; }

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
                    return Races[0]?.Date?.ToShortDateString() ?? "Not dated";
                }

                var maxDate = Races.Max(r => r.Date);
                var minDate = Races.Min(r => r.Date);
                // if one has a value, they should both have values, but for completeness sake:
                if (maxDate.HasValue && minDate.HasValue)
                {
                    if (maxDate?.Date != minDate?.Date)
                    {
                        return minDate?.ToShortDateString() + " - " + maxDate?.ToShortDateString();
                    }
                    else
                    {
                        return minDate?.ToShortDateString();
                    }
                }
                // no max or min date
                return "Not dated";
            }
        }
    }
}

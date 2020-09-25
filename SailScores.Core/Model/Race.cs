using SailScores.Api.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{
    public class Race : IEquatable<Race>
    {
        public Guid Id { get; set; }

        public Guid ClubId { get; set; }
        [StringLength(200)]
        public String Name { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:MMMM d, yyyy}")]
        public DateTime? Date { get; set; }

        // Typically the order of the race for a given date, but may not be.
        // used for display order after date. 
        public int Order { get; set; }
        [StringLength(1000)]
        public String Description { get; set; }
        public String TrackingUrl { get; set; }

        public Fleet Fleet { get; set; }

        public virtual IList<Score> Scores { get; set; }

        public IList<Series> Series { get; set; }

        public Season Season { get; set; }

        public RaceState? State { get; set; }

        public Weather Weather { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public String UpdatedBy { get; set; }

        public bool Equals(Race other)
        {
            return this.Id == other.Id
                && (this.ClubId == other.ClubId)
                && (this.Name == other.Name)
                && (this.Date == other.Date)
                && (this.Order == other.Order)
                && (this.Description == other.Description)
                && (this.Fleet == other.Fleet);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            Race raceObj = obj as Race;
            if (raceObj == null)
                return false;
            else
                return Equals(raceObj);
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}

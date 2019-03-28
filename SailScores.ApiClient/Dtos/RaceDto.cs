using SailScores.Api.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos
{
    public class RaceDto : IEquatable<RaceDto>
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

        public Guid FleetId { get; set; }
        public IList<Guid> ScoreIds { get; set; }
        // Scores is the big exception to Dto opbjects not containing other non-primitive types
        public IList<ScoreDto> Scores { get; set; }
        public IList<Guid> SeriesIds { get; set; }

        public RaceState State { get; set; }

        public bool Equals(RaceDto other)
        {
            return this.Id == other.Id
                && (this.ClubId == other.ClubId)
                && (this.Name == other.Name)
                && (this.Name == other.Name)
                && (this.Date == other.Date)
                && (this.Order == other.Order)
                && (this.Description == other.Description)
                && (this.FleetId == other.FleetId);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            RaceDto raceObj = obj as RaceDto;
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

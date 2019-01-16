using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos
{
    public class ScoreCodeDto
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }
        [StringLength(20)]
        public String Text { get; set; }
        [StringLength(1000)]
        public String Description { get; set; }
        public bool? CountAsCompetitor { get; set; }
        public bool? Discardable { get; set; }
        public bool? UseAverageResult { get; set; }
        public int? CompetitorCountPlus { get; set; }
        
    }
}

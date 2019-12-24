using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{
    public class Season
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }

        [Required]
        [StringLength(200)]
        public String Name { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:Y}")]
        public DateTime Start { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:Y}")]
        public DateTime End { get; set; }

        public IEnumerable<Series> Series { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SailScores.Database.Entities
{
    public class Announcement
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }
        public Guid? RegattaId { get; set; }

        public String Content { get; set; }

        [Column("CreatedDateUtc")]
        public DateTime CreatedDate { get; set; }

        [Column("CreatedDateLocal")]
        public DateTime CreatedLocalDate { get; set; }

        [StringLength(128)]
        public String CreatedBy { get; set; }
        [Column("UpdatedDateUtc")]
        public DateTime? UpdatedDate { get; set; }

        [Column("UpdatedDateLocal")]
        public DateTime? UpdatedLocalDate { get; set; }
        [StringLength(128)]
        public String UpdatedBy { get; set; }

        public DateTime? ArchiveAfter { get; set; }
        public Guid? PreviousVersion { get; set; }

        public bool IsDeleted { get; set; }

    }
}

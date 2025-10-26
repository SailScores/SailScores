using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{
    public class SystemAlert
    {
        public Guid Id { get; set; }

        public string Content { get; set; }

        public DateTime ExpiresUtc { get; set; }

        public DateTime CreatedDate { get; set; }

        [StringLength(128)]
        public string CreatedBy { get; set; }

        public bool IsDeleted { get; set; }
    }
}

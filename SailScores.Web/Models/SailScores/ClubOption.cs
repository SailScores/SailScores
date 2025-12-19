using System;

namespace SailScores.Web.Models.SailScores
{
    public class ClubOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Initials { get; set; }
        public Guid? LogoFileId { get; set; }
    }
}

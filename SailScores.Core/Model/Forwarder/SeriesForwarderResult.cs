using System;

namespace SailScores.Core.Model.Forwarder
{
    public class SeriesForwarderResult
    {
        public Guid Id { get; set; }
        public String OldClubInitials { get; set; }
        public String OldSeasonUrlName { get; set; }
        public String OldSeriesUrlName { get; set; }
        public String NewClubInitials { get; set; }
        public String NewSeasonUrlName { get; set; }
        public String NewSeriesUrlName { get; set; }
    }
}

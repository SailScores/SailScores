using System;

namespace SailScores.Api.Dtos.Public
{
    public class PublicSeriesCompetitorRaceResultDto
    {
        public Guid RaceId { get; set; }

        public int? Place { get; set; }

        public string Code { get; set; }

        public decimal? ScoreValue { get; set; }

        public decimal? PerfectScoreValue { get; set; }

        public bool Discard { get; set; }

        public TimeSpan? ElapsedTime { get; set; }
    }
}

using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Model
{
    public class SeriesAsRace : Race
    {
        private const string UrlSeparator = "/";
        // Same as ScoringPipeline.DEFAULT_CODE.
        public const string AbsentCode = "DNC";

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int TotalChildRaceCount { get; set; } = 0;
        public string SeriesUrl { get; set; }

        // mode is ChildRankAsRace (each competitor's place is their child series rank) or
        // ChildScoreAsRace (each competitor's score is their child series score, used as-is).
        public SeriesAsRace(
            Series childSeries,
            FlatResults flatResults,
            SummaryRollupMode mode = SummaryRollupMode.ChildRankAsRace,
            bool includeUnrankedScores = false
            )
        {
            var minDate = childSeries.Races.Count > 0 ? childSeries.Races.Min(r => r.Date) : null;

            var state = RaceState.Scheduled;
            if (childSeries.Races.Count > 0)
            {
                // Series has races - determine state based on race states
                if (childSeries.Races.Any(r => r.State == RaceState.Raced))
                {
                    state = RaceState.Raced;
                }
                else if (childSeries.Races.All(r => r.State == RaceState.Abandoned))
                {
                    state = RaceState.Abandoned;
                }
                else if (childSeries.Races.All(r => r.State == RaceState.Scheduled))
                {
                    state = RaceState.Scheduled;
                }
                else if (minDate.HasValue && minDate < DateTime.UtcNow)
                {
                    state = RaceState.Raced;
                }
            }
            else
            {
                // Series has no races - determine state based on StartDate
                if (childSeries.StartDate.HasValue && childSeries.StartDate.Value >= DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    // Start date is today or in the future - mark as Scheduled
                    state = RaceState.Scheduled;
                }
                else
                {
                    // No start date or start date is in the past - mark as Abandoned
                    state = RaceState.Abandoned;
                }
            }

            this.Id = childSeries.Id;
            this.ClubId = childSeries.ClubId;
            this.Name = childSeries.Name;

            // Use the actual race date if available, otherwise fall back to series StartDate
            this.Date = minDate ?? (childSeries.StartDate.HasValue 
                ? childSeries.StartDate.Value.ToDateTime(TimeOnly.MinValue) 
                : null);

            this.Order = 1;
            this.Description = childSeries.Description;
            this.State = state;
            this.Scores = new List<Score>();
            this.Series = new List<Series> { childSeries };
            this.Season = childSeries.Season;
            this.UpdatedDate = DateTime.UtcNow;
            this.UpdatedBy = "System";
            this.IsSeriesSummary = true;

            // Set the start and end dates for the series
            // If series has races, use race dates; otherwise use series dates
            if (childSeries.Races.Count > 0)
            {
                this.StartDate = childSeries.Races.Min(r => r.Date);
                this.EndDate = childSeries.Races.Max(r => r.Date);
            }
            else
            {
                // For series with no races, use the series StartDate and EndDate
                this.StartDate = childSeries.StartDate.HasValue 
                    ? childSeries.StartDate.Value.ToDateTime(TimeOnly.MinValue) 
                    : null;
                this.EndDate = childSeries.EndDate.HasValue 
                    ? childSeries.EndDate.Value.ToDateTime(TimeOnly.MinValue) 
                    : null;
            }

            this.TotalChildRaceCount = childSeries.Races.Count;
            this.SeriesUrl = childSeries.Season.UrlName + UrlSeparator + childSeries.UrlName;

            foreach (var result in flatResults?.CalculatedScores ?? [])
            {
                if (mode == SummaryRollupMode.ChildScoreAsRace)
                {
                    var childScore = result.TotalScore ?? result.Average ?? result.PointsEarned;
                    if (childScore != null && (result.Rank != null || includeUnrankedScores))
                    {
                        this.Scores.Add(new Score
                        {
                            Place = result.Rank,
                            PresetScoreValue = childScore,
                            CompetitorId = result.CompetitorId,
                            RaceId = childSeries.Id,
                            Race = this
                        });
                    }
                }
                else if (result.Rank != null)
                {
                    this.Scores.Add(new Score
                    {
                        Place = result.Rank,
                        CompetitorId = result.CompetitorId,
                        RaceId = childSeries.Id,
                        Race = this
                    });
                }
            }


        }

        // ChildScoreAsRace: gives each of these competitors with no score in this race the
        // child series score for missing every race, as a DNC. Codes don't change the value.
        public void AddAbsentCompetitorScores(IEnumerable<Guid> competitorIds, decimal absentScore)
        {
            var scored = new HashSet<Guid>(this.Scores.Select(s => s.CompetitorId));
            foreach (var competitorId in competitorIds.Where(id => !scored.Contains(id)))
            {
                this.Scores.Add(new Score
                {
                    Code = AbsentCode,
                    PresetScoreValue = absentScore,
                    CompetitorId = competitorId,
                    RaceId = this.Id,
                    Race = this
                });
            }
        }
    }
}

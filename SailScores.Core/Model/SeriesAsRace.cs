using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Model
{
    public class SeriesAsRace : Race
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int TotalChildRaceCount { get; set; } = 0;
        public string SeriesUrl { get; set; }

        public SeriesAsRace(
            Series childSeries,
            FlatResults flatResults
            )
        {
            var minDate = childSeries.Races.Count > 0 ? childSeries.Races.Min(r => r.Date) : null;

            var state = RaceState.Scheduled;
            if (childSeries.Races
                .Any(r => r.State == RaceState.Raced))
            {
                state = RaceState.Raced;
            } else if(childSeries.Races.All(r => r.State == RaceState.Abandoned))
            {
                state = RaceState.Abandoned;
            } else if(childSeries.Races.All(r => r.State == RaceState.Scheduled))
            {
                state = RaceState.Scheduled;
            } else if(minDate.HasValue && minDate < DateTime.UtcNow)
            {
                state = RaceState.Raced;
            }

            this.Id = childSeries.Id;
            this.ClubId = childSeries.ClubId;
            this.Name = childSeries.Name;
            this.Date = minDate;
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
            this.StartDate = childSeries.Races.Count > 0 ? childSeries.Races.Min(r => r.Date) : null;
            this.EndDate = childSeries.Races.Count > 0 ? childSeries.Races.Max(r => r.Date) : null;
            this.TotalChildRaceCount = childSeries.Races.Count;
            this.SeriesUrl = childSeries.Season.UrlName + "/" + childSeries.UrlName;

            foreach (var result in flatResults?.CalculatedScores ?? [])
            {
                Score score;
                if (result.Rank != null)
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
    }
}

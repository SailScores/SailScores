using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring
{
    public class SeriesCalculator : ISeriesCalculator
    {
        public SeriesResults CalculateResults(Series series)
        {
            var returnResults = new SeriesResults
            {
                Races = series.Races.OrderBy(r => r.Date).ThenBy(r => r.Order).ToList(),
                Competitors = series
                    .Races
                    .SelectMany(
                        r => r
                            .Scores
                            .Select(s => s.Competitor))
                    .Distinct()
                    .ToList(),
                Results = new Dictionary<Competitor, SeriesCompetitorResults>()
            };

            SetScores(returnResults,
                series
                .Races
                .SelectMany(
                    r => r
                        .Scores));
            return returnResults;
        }

        public void SetScores(SeriesResults resultsWorkInProgress, IEnumerable<Score> scores)
        {
            ValidateScores(resultsWorkInProgress, scores);

            foreach (var comp in resultsWorkInProgress.Competitors)
            {
                SeriesCompetitorResults compResults = GenerateBasicScores(comp, scores);
                CalculateCodedResults(resultsWorkInProgress, compResults);
                DiscardScores(resultsWorkInProgress, compResults);
                CalculateTotal(compResults);

                resultsWorkInProgress.Results[comp] = compResults;
            }

            CalculateRanks(resultsWorkInProgress);
            resultsWorkInProgress.Competitors = ReorderCompetitors(resultsWorkInProgress);
        }

        private IList<Competitor> ReorderCompetitors(SeriesResults results)
        {
            return results.Competitors.OrderBy(c => results.Results[c].Rank).ToList();
        }

        private void CalculateRanks(SeriesResults resultsWorkInProgress)
        {
            var orderedComps = resultsWorkInProgress.Results.Values.OrderBy(s => s.TotalScore ?? Decimal.MaxValue);

            int i = 1;
            foreach (var comp in orderedComps)
            {
                comp.Rank = i;

                i++;
            }
        }

        private void CalculateCodedResults(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
        {
            
            //TODO Not completely implemented.

            //Fill in DNCs
            foreach (var race in resultsWorkInProgress.Races)
            {
                if (!compResults.CalculatedScores.ContainsKey(race))
                {
                    compResults.CalculatedScores.Add(race,
                        new CalculatedScore
                        {
                            RawScore = new Score
                            {
                                Code ="DNC",
                                Competitor = compResults.Competitor,
                                Race = race
                            },
                            // this line needs a where "Counts as started"
                            ScoreValue = race.Scores.Count + 2
                        });
                }
            }
        }

        private void CalculateTotal(SeriesCompetitorResults compResults)
        {
            compResults.TotalScore = compResults.CalculatedScores.Values.Sum(s => !s.Discard ? ( s.ScoreValue ?? 0.0m) : 0.0m);
        }

        private void DiscardScores(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
        {
            int numOfDiscards = GetNumberOfDiscards(resultsWorkInProgress);

            // todo Check for non-discardable codes
            var compResultsOrdered = compResults.CalculatedScores.Values.OrderByDescending(s => s.ScoreValue);
            foreach (var score in compResultsOrdered.Take(numOfDiscards))
            {
                score.Discard = true;
            }
        }

        private int GetNumberOfDiscards(SeriesResults resultsWorkInProgress)
        {
            //TODO put a real formula in here. (need to figure out series scoring settings.)
            return resultsWorkInProgress.Races.Count / 3;
        }


        public void ValidateScores(SeriesResults results, IEnumerable<Score> scores)
        {
            bool allRacesFound = scores.All(s => results.Races.Any(r => r.Id == s.RaceId));
            bool allCompetitorsFound = scores.All(s => results.Competitors.Any(c => c.Id == s.CompetitorId));
            if (!allRacesFound)
            {
                throw new InvalidOperationException(
                    "A score for a race that is not in the series was provided to SeriesCalculator");
            }

            if (!allCompetitorsFound)
            {
                throw new InvalidOperationException(
                    "A score for a competitor that is not in the series was provided to SeriesCalculator");

            }
        }

        private SeriesCompetitorResults GenerateBasicScores(Competitor comp, IEnumerable<Score> scores)
        {
            var returnResults = new SeriesCompetitorResults
            {
                Competitor = comp,
                CalculatedScores = new Dictionary<Race, CalculatedScore>()
            };
            foreach (var score in scores.Where(s => s.Competitor == comp))
            {
                returnResults.CalculatedScores[score.Race] = new CalculatedScore
                {
                    Discard = false,
                    RawScore = score,
                    ScoreValue = score.Place
                };
            }

            return returnResults;
        }
    }

}




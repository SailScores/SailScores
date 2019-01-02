using Sailscores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailscores.Core.Scoring
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
                            ScoreValue = race.Scores.Where(s => CountsAsStarted(s)).Count() + 2
                        });
                }
            }
            //calculate non-average codes first

            //then calculate average codes
            foreach (var race in resultsWorkInProgress.Races)
            {
                var score = compResults.CalculatedScores[race];
                if (score != null && !String.IsNullOrWhiteSpace(score.RawScore.Code))
                {
                    switch (score.RawScore.Code.ToUpperInvariant())
                    {
                        case "SB":
                        case "RC":
                        case "ORA":
                            score.ScoreValue = CalculateAverage(compResults);
                            break;
                        //default:
                        //    throw new InvalidOperationException($"Unknown Score Code: {score.RawScore.Code}");
                    }
                }
            }

        }

        private decimal? CalculateAverage(SeriesCompetitorResults compResults)
        {
            //int numRealResults = compResults.CalculatedScores
            //        .Values.Count(s =>
            //            String.IsNullOrWhiteSpace(s.RawScore.Code));
            int numAverages = compResults.CalculatedScores
                    .Values.Count(s =>
                        IsAverage(s.RawScore.Code));
            int discards = GetNumberOfDiscards(compResults.CalculatedScores.Count);

            var average = compResults.CalculatedScores.Values
                .Where(s => (s.ScoreValue ?? 0m) != 0m && !IsAverage(s.RawScore.Code))
                .OrderBy(s => s.ScoreValue)
                .Take(compResults.CalculatedScores.Count - numAverages - discards)
                .Average(s => s.ScoreValue) ?? 0m;

            return Math.Round(average, 1);

        }

        private bool IsAverage(string code)
        {
            if (String.IsNullOrWhiteSpace(code))
            {
                return false;
            }
            switch (code.ToUpperInvariant())
            {
                case "SB":
                case "RC":
                case "ORA":
                    return true;
                default:
                    return false;
            }
        }

        private bool CountsAsStarted(Score s)
        {
            if (String.IsNullOrWhiteSpace(s.Code) &&
                (s.Place ?? 0) != 0)
            {
                return true;
            }
            switch (s.Code.ToUpperInvariant())
            {
                case "DNF":
                case "OCS":
                case "DSQ":
                // Lake harriet, a boat counts as racing if they left their buoy to race:
                case "DNS":
                    return true;
                default:
                    return false;
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
            return GetNumberOfDiscards(resultsWorkInProgress.Races.Count);

        }
        private int GetNumberOfDiscards(int numberOfRaces)
        {
            if (numberOfRaces > 4)
            {
                return numberOfRaces / 3;
            }
            return 0;
        }


        public void ValidateScores(SeriesResults results, IEnumerable<Score> scores)
        {
            bool allRacesFound = scores.All(s => results.Races.Any(
                r => r.Id == s.RaceId
                    || r == s.Race));
            bool allCompetitorsFound = scores.All(s => results.Competitors.Any(
                c => c.Id == s.CompetitorId
                    || c == s.Competitor ));
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




using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring;


// Scoring system for Top X high points as described by VARC for their series.

// first place receive 100, second 99, etc.
// Except if there are less than 5 boats participating, the max score is decreased
// by a few. Look at the "GetHundredBaseScore(...)" method for the exact formula.

// Not sure how long this will be in SailScores, so for now, a hack:
// the number of races to keep will be stored in the scoring system's 
// participation required field.

public class TopXHighPointCalculator : BaseScoringCalculator
{
    public TopXHighPointCalculator(ScoringSystem scoringSystem) : base(scoringSystem)
    {
        CompetitorComparer = new HighPointSeriesCompComparer();
    }

    protected override decimal? GetBasicScore(IEnumerable<Score> allScores, Score currentScore)
    {
        int starters = allScores.Count(s =>
            s.Race == currentScore.Race
             && CountsAsStarted(s));


        int baseScore =
            allScores
                .Count(s =>
                    currentScore.Place.HasValue
                    && s.Race == currentScore.Race
                    && s.Place <= currentScore.Place
                    && !ShouldAdjustOtherScores(s)
                    );

        // removed code to handle race result ties: this should treat both as the same place.

        return GetHundredBasedScore(baseScore, starters);
    }

    protected override decimal? GetPerfectScore(IEnumerable<Score> allScores, Score currentScore)
    {
        return GetHundredBasedScore(1, allScores.Count());
    }

    // for coded results that counted as a start; they depend on number of starters for this race.
    protected override void CalculateRaceDependentScores(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
    {
        
        foreach (var race in resultsWorkInProgress.SailedRaces)
        {
            var score = compResults.CalculatedScores[race];
            var scoreCode = GetScoreCode(score.RawScore);
            if (scoreCode != null && CameToStart(score.RawScore)
                && scoreCode.Formula != TIE_FORMULANAME)
            {
                var starters = race.Scores.Count(s => CountsAsStarted(s));
                score.ScoreValue = GetHundredBasedScore(starters + 1, starters);
            }
        }
    }

    // The series score for each boat:
    // Total the top "participationPercent" scores: need to force that to an int <summary>
    protected override void CalculateTotals(
        SeriesResults results,
        IEnumerable<Score> scores)
    {
        // badly named: this tells the UI to not use the score for first/second/third
        results.IsPercentSystem = true;

        // This line below needs a revamp if this system will be around for long (written Oct 2024...)
        // need to get it into a properly named field in the scoring system.
        results.PercentRequired = 0;
        var racesToInclude = (int)(ScoringSystem.ParticipationPercent ?? 5m);
        var raceCount = results.Races.Where(r => (r.State ?? RaceState.Raced) == RaceState.Raced
            || r.State == RaceState.Preliminary).Count();

        Dictionary<Guid, int> starterCounts = new Dictionary<Guid, int>();
        foreach (Race r in results.SailedRaces)
        {
            starterCounts[r.Id] = r.Scores.Count(s => CountsAsStarted(s));
        }
        foreach (var comp in results.Competitors)
        {
            var currentCompResults = results.Results[comp];
            var racesParticipated = currentCompResults.CalculatedScores
                .Where(s => CountsAsStarted(s.Value.RawScore) ||
                       CountsAsParticipation(s.Value.RawScore)).Count();

            // take the top x scores, so order them high to low, then take


            // racesToExclude should include discards and DNCs
            var scoresToCount = currentCompResults
                .CalculatedScores
                .OrderByDescending(s => s.Value.ScoreValue)
                .Take(racesToInclude)
                .Select(kvp => kvp.Value);
                //                .Where(s => s.Value.Discard ||
                //                ( !String.IsNullOrEmpty(s.Value.RawScore.Code) && !(GetScoreCode(s.Value.RawScore.Code).CameToStart ?? false)))
                //.Select(s => s.Key);

            
            var compTotal = scoresToCount.Sum(scoresToCount => scoresToCount.ScoreValue ?? 0.0m);

            currentCompResults.PointsEarned = compTotal;
            currentCompResults.TotalScore = compTotal;
            //currentCompResults.PointsPossible = perfectScore;

        }
    }

    // Discards for Cox-Sprague are not straight forward.
    protected override void DiscardScores(
        SeriesResults resultsWorkInProgress,
        SeriesCompetitorResults compResults)
    {

        // Discards don't make sense in this system: any score will help, since total is
        // additive.
        //
        // int numOfDiscards = GetNumberOfDiscards(resultsWorkInProgress, compResults);

    }   

    protected override void CalculateOverrides(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
    {
        foreach (var race in resultsWorkInProgress.SailedRaces)
        {
            var score = compResults.CalculatedScores[race];
            var defaultScore = GetDefaultScore(race, resultsWorkInProgress);
            if (score?.ScoreValue != null && score.ScoreValue < defaultScore)
            {
                score.ScoreValue = defaultScore;
            }
        }
    }

    protected override decimal? GetDefaultScore(Race race, SeriesResults resultsWorkInProgress)
    {
        return 0;
    }

    protected override decimal? GetPenaltyScore(CalculatedScore score, Race race, ScoreCode scoreCode)
    {
        var dnfScore = GetDnfScore(race) ?? 0;
        var fleetSize = race.Scores.Where(s => CameToStart(s)).Count();
        var percentAdjustment = Convert.ToDecimal(scoreCode?.FormulaValue ?? 20);
        var percent = Math.Round(fleetSize * percentAdjustment / 100m, MidpointRounding.AwayFromZero);

        return Math.Max(dnfScore, (score.ScoreValue ?? 0) - percent);
    }

    protected override ScoreCodeSummary GetScoreCodeSummary(string code)
    {
        var codeDef = GetScoreCode(code);

        string possibleFormula;

        if (CountsAsParticipation(codeDef) && !CameToStart(codeDef))
        {
            possibleFormula = "Participation but not scored.";
        }
        else if (!CameToStart(codeDef))
        {
            possibleFormula = "No participation, no score."; 
        } else
        {
            possibleFormula = base.GetScoreCodeSummary(code).Formula;
        }
        if (possibleFormula.Contains("Fixed") && possibleFormula.Contains("0"))
        {
            possibleFormula = "One greater than # came to start.";
        }

        return new ScoreCodeSummary
        {
            Name = codeDef.Name,
            Description = codeDef.Description,
            Formula = possibleFormula
        };

    }

    private decimal? GetHundredBasedScore(int baseScore, int starters)
    {
        if (starters < 5)
        {
            return 101 - (5 - starters) - baseScore;
        }
        return 101 - baseScore;
    }
}

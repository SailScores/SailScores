using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SailScores.Core.Scoring;

// even though this class inherits from AppendixACalculator,
// it is still considered a "Base" system: it does not inherit
// scoring codes from any system.

public class AppAAltFirstIsZero : AppendixACalculator
{
    public AppAAltFirstIsZero(ScoringSystem scoringSystem) : base(scoringSystem)
    {
        CompetitorComparer = new LowPointSeriesCompComparer();
    }

    protected override decimal? GetBasicScore(
        IEnumerable<Score> allScores,
        Score currentScore)
    {
        if (currentScore == null)
        {
            throw new ArgumentNullException(nameof(currentScore));
        }

        decimal? returnScore =
            Convert.ToDecimal(allScores
                .Count(s =>
                    currentScore.Place.HasValue
                    && s.Race == currentScore.Race
                    && s.Place < currentScore.Place
                    && !ShouldAdjustOtherScores(s)
                    ) + 1);

        // usually if one of these conditions is true, the other is true as well,
        // but this might catch some edge cases.
        if (currentScore.Place == 1 && returnScore == 1)
        {
            returnScore = 0;
        }

        returnScore = GetTiedScore(allScores, currentScore) ?? returnScore;

        return returnScore;
    }

    // There are two scenarios to handle for ties:
    // 1. Place: 2 code: null & Place: 2 code: "TIE"
    // 2. Place: 2 code: null & Place: 3 code: "TIE"
    // That is, the places might or might not match.
    private decimal? GetTiedScore(IEnumerable<Score> allScores, Score currentScore)
    {

        // other results with same place are ties
        int numTied = allScores.Count(s =>
            currentScore.Place.HasValue
            && s.Race == currentScore.Race
            && s.Place == currentScore.Place
            && !ShouldAdjustOtherScores(s));


        int tieBase = currentScore.Place ?? 1;
        if (numTied == 1)
        {
            var tmpScore = currentScore;
            bool tie = false;
            // if current is tie look at previous scores until not a tie.
            if ((GetScoreCode(currentScore?.Code)?.Formula ?? String.Empty) == "TIE")
            {
                do
                {
                    numTied++;
                    var previousScore = GetPreviousScore(allScores, tmpScore);
                    tieBase = previousScore?.Place ?? tieBase;
                    tie = (GetScoreCode(previousScore?.Code)?.Formula ?? String.Empty) == "TIE";
                    tmpScore = previousScore;

                } while (tie);
            }

            //also need to look at next scores to see if they are ties.
            tmpScore = currentScore;
            do
            {
                var nextScore = GetNextScore(allScores, tmpScore);
                tie = (GetScoreCode(nextScore?.Code)?.Formula ?? String.Empty) == "TIE";
                if (tie)
                {
                    numTied++;
                }
                tmpScore = nextScore;

            } while (tie);
        }

        if (numTied > 1)
        {
            int total = 0;
            for (int i = 0; i < numTied; i++)
            {
                // normally a tie is the average of the scores, but for this method,
                // need to allow first place to be zero.
                if (tieBase == 1 && i == 0)
                {
                    total += 0;
                }
                else
                {
                    total += (tieBase + i);
                }
            }
            return (decimal)total / (decimal)numTied;
        }

        return null;
    }

}

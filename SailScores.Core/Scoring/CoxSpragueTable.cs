using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SailScores.Core.Scoring
{
    internal static class CoxSpragueTable
    {
        public static int GetScore(int place, int startersInRace)
        {
            if (place > startersInRace + 1)
            {
                throw new ArgumentOutOfRangeException("place must be less than or equal to competitorsInRace + 1");
            }

            if (startersInRace < 2)
            {
                return 0;
            }

            if (place < 1)
            {
                return 0;
            }

            var competitorsToUse = startersInRace;
            if (startersInRace > 20)
            {
                competitorsToUse = 20;
            }

            if (place > 21)
            {
                return Math.Max(79-place, 0);
            }
            return PlaceToScoreTable[competitorsToUse - 2][place - 1];
        }

        static int[][] PlaceToScoreTable =
        {
            //2 competitors
            new [] { 10, 4 , 0 },
            new [] { 31, 25 , 21, 17 },
            new [] { 43, 37, 33, 29, 26 },
            new [] { 52, 46, 42, 38, 35, 32 },
            new [] { 60, 54, 50, 46, 43, 40, 30 },
            new [] { 66, 60, 56, 52, 49, 46, 44, 42 },
            new [] { 72, 66, 62, 58, 55, 52, 50, 48, 46  },
            new [] { 76, 70, 66, 62, 59, 56, 54, 52, 50, 48 },
            //10 competitors
            new [] { 80, 74, 70, 66, 63, 60, 58, 56, 54, 52, 50 },
            new [] { 84, 78, 74, 70, 67, 64, 62, 60, 58, 56, 54, 52 },
            new [] { 87, 81, 77, 73, 70, 67, 65, 63, 61, 59, 57, 55, 53 },
            new [] { 90, 84, 80, 76, 73, 70, 68, 66, 64, 62, 60, 58, 56, 55 },
            new [] { 92, 86, 82, 78, 75, 72, 70, 68, 66, 64, 62, 60, 58, 57, 56 },
            new [] { 94, 88, 84, 80, 77, 74, 72, 70, 68, 66, 64, 62, 60, 59, 58, 57 },
            new [] { 96, 90, 86, 82, 79, 76, 74, 72, 70, 68, 66, 64, 62, 61, 60, 59, 58 },
            new [] { 97, 91, 87, 83, 80, 77, 75, 73, 71, 69, 67, 65, 63, 62, 61, 60, 59, 58},
            new [] { 98, 92, 88, 84, 81, 78, 76, 74, 72, 70, 68, 66, 64, 63, 62, 61, 60, 59, 58},
            new [] { 99, 93, 89, 85, 82, 79, 77, 75, 73, 71, 69, 67, 65, 64, 63, 62, 61, 60, 59, 58 },
            //20 competitors
            new [] { 100, 94, 90, 86, 83, 80, 78, 76, 74, 72, 70, 68, 66, 65, 64, 63, 62, 61, 60, 59, 58 }
        };
    }
}

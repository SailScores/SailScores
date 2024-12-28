using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Utility;

internal class AlphaNumericComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        // if the first word is numeric on both, compare as numbers
        if (x != null && y != null && x.Length > 0 && y.Length > 0)
        {
            var xWord = x.Trim().Split(' ')[0];
            var yWord = y.Trim().Split(' ')[0];
            if (xWord.Length > 0 && yWord.Length > 0 && xWord.All(char.IsDigit) && yWord.All(char.IsDigit))
            {
                return int.Parse(xWord).CompareTo(int.Parse(yWord));
            }

            var firstWordComparison = string.Compare(xWord, yWord, StringComparison.Ordinal);
            if (firstWordComparison != 0 || String.IsNullOrWhiteSpace(xWord) || String.IsNullOrWhiteSpace(yWord))
            {
                return firstWordComparison;
            }
            return Compare(x.Substring(xWord.Length), y.Substring(yWord.Length));
        }
        return string.Compare(x, y, StringComparison.Ordinal);
    }
}

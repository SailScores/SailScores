using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SailScores.Core.Utility;

public static class ClubDateFormatUtility
{
    private static readonly HashSet<string> AllowedTokens = new(StringComparer.Ordinal)
    {
        "d",
        "dd",
        "M",
        "MM",
        "MMM",
        "MMMM",
        "yy",
        "yyyy"
    };

    public static bool TryNormalize(string input, out string normalized, out string errorMessage)
    {
        normalized = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        var candidate = input.Trim().Replace('Y', 'y').Replace('D', 'd');

        if (candidate.Length > 30)
        {
            errorMessage = "Date format must be 30 characters or less.";
            return false;
        }

        if (!HasOnlyAllowedCharacters(candidate))
        {
            errorMessage = "Date format can only use d, M, y tokens and separators (/ - . space).";
            return false;
        }

        if (!HasOnlyAllowedTokens(candidate))
        {
            errorMessage = "Unsupported token. Allowed: d, dd, M, MM, MMM, MMMM, yy, yyyy.";
            return false;
        }

        normalized = candidate;
        return true;
    }

    private static bool HasOnlyAllowedCharacters(string value)
    {
        foreach (var c in value)
        {
            if (char.IsWhiteSpace(c) || c == '/' || c == '-' || c == '.')
            {
                continue;
            }

            if (c is 'd' or 'M' or 'y')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool HasOnlyAllowedTokens(string value)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();

        foreach (var c in value)
        {
            if (c is 'd' or 'M' or 'y')
            {
                current.Append(c);
                continue;
            }

            if (current.Length > 0)
            {
                tokens.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        if (tokens.Count < 2)
        {
            return false;
        }

        return tokens.All(t => AllowedTokens.Contains(t));
    }
}

using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Services;

/// <summary>
/// Simple implementation of matching logic extracted from Web layer.
/// This will be the place to add robust matching algorithms and unit tests.
/// </summary>
public class MatchingService : IMatchingService
{
    public IEnumerable<MatchingSuggestion> GetSuggestions(string ocrText, IEnumerable<Competitor> competitors)
    {
        var suggestions = new List<MatchingSuggestion>();
        if (string.IsNullOrWhiteSpace(ocrText) || competitors == null || !competitors.Any())
        {
            return suggestions;
        }

        // Normalize OCR quirks but keep spaces/hyphens for tokenization rules
        var normalizedInput = Normalize(ocrText);

        // Extract candidate tokens from input
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in ExtractAlphaNumericCandidates(normalizedInput))
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                candidates.Add(token);
                var trimmed = TrimLeadingZeros(token);
                if (!string.IsNullOrWhiteSpace(trimmed)) candidates.Add(trimmed);
            }
        }
        foreach (var num in ExtractNumericSequences(normalizedInput))
        {
            if (!string.IsNullOrWhiteSpace(num))
            {
                candidates.Add(num);
                var trimmed = TrimLeadingZeros(num);
                if (!string.IsNullOrWhiteSpace(trimmed)) candidates.Add(trimmed);
            }
        }

        if (candidates.Count == 0)
        {
            return suggestions;
        }

        // Local helpers for normalization for compare
        string Canon(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            // Apply OCR normalization then remove common separators
            var v = s.Trim().ToUpperInvariant();
            return v.Replace(" ", string.Empty)
                .Replace("-", string.Empty);
        }

        // Build competitor index of possible sail numbers (main + alt)
        var compNums = new List<(Competitor comp, string number, bool isAlt)>();
        foreach (var comp in competitors)
        {
            var main = Canon(Normalize(comp.SailNumber ?? string.Empty));
            if (!string.IsNullOrEmpty(main)) compNums.Add((comp, main, false));
            var alt = Canon(Normalize(comp.AlternativeSailNumber ?? string.Empty));
            if (!string.IsNullOrEmpty(alt)) compNums.Add((comp, alt, true));
        }

        // Exact matches first
        foreach (var cand in candidates)
        {
            var cc = Canon(cand);
            if (string.IsNullOrEmpty(cc)) continue;

            foreach (var item in compNums)
            {
                if (string.Equals(item.number, cc, StringComparison.OrdinalIgnoreCase))
                {
                    var confidence = item.isAlt ? 0.98 : 1.0; // prefer main exact
                    suggestions.Add(new MatchingSuggestion(item.comp, confidence, cc, true));
                }
            }
        }

        // Suffix (ends-with) matches for partial OCR captures like last digits
        var partialCandidates = new List<(Competitor comp, string matched, double score)>();
        foreach (var cand in candidates)
        {
            var cc = Canon(cand);
            if (string.IsNullOrEmpty(cc)) continue;

            foreach (var item in compNums)
            {
                if (item.number.Length > cc.Length && item.number.EndsWith(cc, StringComparison.OrdinalIgnoreCase))
                {
                    // Score based on completeness of the candidate relative to full number
                    var completeness = (double)cc.Length / Math.Max(1, item.number.Length);
                    partialCandidates.Add((item.comp, cc, completeness));
                }
                if(cc.Length == 3 && cc.EndsWith("1") || cc.Length >= 4)
                {
                    var ccBegin = cc.Substring(0, cc.Length - 1);
                    if (item.number.Length > ccBegin.Length && item.number.EndsWith(ccBegin, StringComparison.OrdinalIgnoreCase))
                    {
                        // Score based on completeness of the candidate relative to full number, but lower since we dropped a char
                        var completeness = ((double)ccBegin.Length / Math.Max(1, item.number.Length)) - 0.2;
                        partialCandidates.Add((item.comp, ccBegin, completeness));
                    }
                }
            }
        }


        var orderedSuffix = partialCandidates
            .GroupBy(s => (s.comp.Id, s.matched))
            .Select(g => g.OrderByDescending(x => x.score).First()) // best score per (comp, candidate)
            .OrderByDescending(s => s.score)
            .ThenBy(s => s.comp.SailNumber?.Length ?? int.MaxValue)
            .Select(s => new MatchingSuggestion(
                s.comp,
                // Map completeness into a reasonable confidence range
                Math.Min(0.95, Math.Max(0.3, 0.3 + 0.3 * s.score)),
                s.matched,
                false));


        suggestions.AddRange(orderedSuffix);

        // Dedupe by competitor id, keep highest confidence, prefer exact
        var dedup = suggestions
            .GroupBy(s => s.Competitor.Id)
            .Select(g => g
                .OrderByDescending(x => x.IsExactMatch)
                .ThenByDescending(x => x.Confidence)
                .First())
            .OrderByDescending(s => s.Confidence)
            .ToList();

        return dedup;
    }

    private string Normalize(string text)
    {
        return text
            .ToUpperInvariant()
            .Replace("O", "0", StringComparison.OrdinalIgnoreCase)
            .Replace("L", "1", StringComparison.OrdinalIgnoreCase)
            .Replace("I", "1", StringComparison.OrdinalIgnoreCase)
            .Replace("|", "1", StringComparison.OrdinalIgnoreCase)
            .Replace("/", "1", StringComparison.OrdinalIgnoreCase)
            .Replace("\\", "1", StringComparison.OrdinalIgnoreCase)
            .Replace("S", "5", StringComparison.OrdinalIgnoreCase)
            .Replace("$", "5", StringComparison.OrdinalIgnoreCase)
            .Replace(".", "", StringComparison.OrdinalIgnoreCase)
            .Replace(",", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    // Extract numeric sequences (e.g., "1234") without regex
    private static IEnumerable<string> ExtractNumericSequences(string text)
    {
        var list = new List<string>();
        int i = 0;
        while (i < text.Length)
        {
            if (char.IsDigit(text[i]))
            {
                int start = i;
                while (i < text.Length && char.IsDigit(text[i])) i++;
                var possible = text.Substring(start, i - start);
                list.Add(possible);
            }
            else
            {
                i++;
            }
        }
        return list;
    }

    // Extract tokens like "ABC1234" when input might be "ABC-1234" or "ABC1234"
    // Only join a short alpha prefix (1-4 letters) with following digits across a single space or hyphen.
    private static IEnumerable<string> ExtractAlphaNumericCandidates(string text)
    {
        var results = new List<string>();
        int i = 0;
        while (i < text.Length)
        {
            // Try to read a short alpha prefix
            int lettersStart = i;
            int lettersLen = 0;
            while (i < text.Length && char.IsLetter(text[i]) && lettersLen < 4)
            {
                i++; lettersLen++;
            }

            if (lettersLen > 0 && lettersLen <= 4)
            {
                // Optional single space or hyphen
                int j = i;
                if (j < text.Length && (text[j] == ' ' || text[j] == '-')) j++;

                // Must be followed by at least one digit to join
                int digitsStart = j;
                int digitsLen = 0;
                while (j < text.Length && char.IsDigit(text[j]))
                {
                    j++; digitsLen++;
                }

                if (digitsLen > 0)
                {
                    var letters = text.Substring(lettersStart, lettersLen);
                    var digits = text.Substring(digitsStart, digitsLen);
                    results.Add(letters + digits);
                    i = j; // advance past the digits we consumed
                    continue;
                }
                else
                {
                    // Not a valid alpha+digits token; step back to just after the start to continue
                    i = lettersStart + 1;
                    continue;
                }
            }

            // If not starting with letters, advance one
            i = lettersStart + 1;
        }
        return results;
    }

    private static string TrimLeadingZeros(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return token;

        // Split into optional alpha prefix (<=4) and numeric tail
        int prefixLen = 0;
        while (prefixLen < token.Length && prefixLen < 4 && char.IsLetter(token[prefixLen]))
        {
            prefixLen++;
        }

        string prefix = token.Substring(0, prefixLen);
        string rest = token.Substring(prefixLen);
        if (rest.Length == 0)
        {
            // No digits, return as-is
            return token;
        }

        int i = 0;
        while (i < rest.Length && rest[i] == '0') i++;
        var trimmedDigits = i >= rest.Length ? "0" : rest.Substring(i);
        return prefix + trimmedDigits;
    }
}

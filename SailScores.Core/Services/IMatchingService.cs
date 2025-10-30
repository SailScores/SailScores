using SailScores.Core.Model;
using System.Collections.Generic;

namespace SailScores.Core.Services;

public record MatchingSuggestion(Competitor Competitor, double Confidence, string MatchedText, bool IsExactMatch);

public interface IMatchingService
{
    /// <summary>
    /// Return ordered suggestions for a given OCR text line against the provided competitors.
    /// Suggestions are ordered best-first.
    /// </summary>
    IEnumerable<MatchingSuggestion> GetSuggestions(string text, IEnumerable<Competitor> competitors);
}

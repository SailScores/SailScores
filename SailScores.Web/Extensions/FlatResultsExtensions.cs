using SailScores.Core.FlatModel;

namespace SailScores.Web.Extensions;

public static class FlatResultsExtensions
{
    // "50% participation (10 races) required for ranking". Results saved before the race
    // count was recorded show only the percent.
    public static string ParticipationRequiredText(this FlatResults results)
    {
        var percent = $"{results.PercentRequired?.ToString("G5")}%";
        if (results.RacesRequired is int races && races > 0)
        {
            var raceLabel = races == 1 ? "race" : "races";
            return $"{percent} participation ({races} {raceLabel}) required for ranking";
        }
        return $"{percent} participation required for ranking";
    }
}

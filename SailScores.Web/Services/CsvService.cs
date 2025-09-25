using Humanizer;
using Microsoft.Extensions.Localization;
using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Web.Resources;
using System.IO;
using System.Text;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class CsvService : ICsvService, IDisposable
{
    private MemoryStream stream;
    private StreamWriter streamWriter;
    private IStringLocalizer<SharedResource> _stringLocalizer;
    private readonly ILocalizerService _sailscoresLocalizer;
    private const string _separator = ",";
    private const string _quote = "\"";

    public CsvService(
        IStringLocalizer<SharedResource> localizer,
        ILocalizerService sailscoresLocalizer)
    {
        _stringLocalizer = localizer;
        _sailscoresLocalizer = sailscoresLocalizer;
    }

    public Stream GetCsv(Series series)
    {
        if (stream != null)
        {
            stream.Dispose();
        }
        stream = new MemoryStream();
        if (streamWriter != null)
        {
            streamWriter.Dispose();
        }
        streamWriter = new StreamWriter(stream, System.Text.Encoding.UTF8);

        streamWriter.WriteLine(GetHeaders(series));
        var compList = series.FlatResults?.Competitors;
        if (compList != null)
            foreach (var comp in compList)
            {
                streamWriter.WriteLine(GetCompResults(series, comp));
            }
        streamWriter.Flush();
        stream.Position = 0;

        return stream;
    }

    private string GetHeaders(Series series)
    {
        var sb = new StringBuilder();
        sb.Append(GetEscapedLocalizedValue("Place"));
        sb.Append(_separator);
        sb.Append(GetEscapedLocalizedValue("Sail"));
        sb.Append(_separator);
        sb.Append(GetEscapedLocalizedValue("Helm"));
        sb.Append(_separator);
        sb.Append(GetEscapedLocalizedValue("Boat"));
        sb.Append(_separator);
        sb.Append(GetEscapedLocalizedValue("Total"));
        sb.Append(_separator);
        foreach (var race in series.FlatResults?.Races ?? Enumerable.Empty<FlatRace>())
        {
            sb.Append(GetEscapedValue(_sailscoresLocalizer.GetShortName(race)));
            sb.Append(_separator);
        }

        return sb.ToString();
    }

    private string GetCompResults(Series series, FlatCompetitor comp)
    {
        var sb = new StringBuilder();
        var score = series.FlatResults.GetScore(comp);
        sb.Append(GetEscapedValue(score?.Rank?.ToString()));
        sb.Append(_separator);
        if ((series.PreferAlternativeSailNumbers ?? false)
            && !String.IsNullOrWhiteSpace(comp.AlternativeSailNumber))
        {
            sb.Append(GetEscapedValue(comp.AlternativeSailNumber));
        }
        else
        {
            sb.Append(GetEscapedValue(comp.SailNumber));
        }
        sb.Append(_separator);
        sb.Append(GetEscapedValue(comp.Name));
        sb.Append(_separator);
        sb.Append(GetEscapedValue(comp.BoatName));
        sb.Append(_separator);

        var totalString = String.Format("{0:0.##}", score?.TotalScore ?? 0);
        sb.Append(GetEscapedValue(totalString.ToString()));
        sb.Append(_separator);
        foreach (var race in series.FlatResults?.Races ?? Enumerable.Empty<FlatRace>())
        {
            if (race.State == RaceState.Scheduled)
            {
                sb.Append(GetEscapedValue("Sched"));
            }
            else if (race.State == RaceState.Abandoned)
            {
                sb.Append(GetEscapedValue("Aband"));
            }
            else
            {
                var raceScore = series.FlatResults.GetScore(comp, race);
                var cellSb = new StringBuilder();
                cellSb.Append(raceScore.Code);
                cellSb.Append(" ");
                cellSb.Append((raceScore.ScoreValue ?? raceScore.Place)?.ToString("N1"));
                if (series.FlatResults.IsPercentSystem && raceScore.Place.HasValue)
                {
                    cellSb.Append(" (");
                    cellSb.Append(raceScore.Place.Value.Ordinalize());
                    cellSb.Append(")");
                }
                sb.Append(GetEscapedValue(cellSb.ToString()));

                if (raceScore.Discard)
                {
                    sb.Append(" ");
                    sb.Append(_stringLocalizer["Discard"]);
                }
            }
            sb.Append(_separator);
        }

        return sb.ToString();
    }


    public Stream GetCsv(IDictionary<string, IEnumerable<Competitor>> competitors)
    {
        if (stream != null)
        {
            stream.Dispose();
        }
        stream = new MemoryStream();
        if (streamWriter != null)
        {
            streamWriter.Dispose();
        }
        streamWriter = new StreamWriter(stream, System.Text.Encoding.UTF8);

        streamWriter.WriteLine(GetCompetitorHeaders());

        foreach (var fleet in competitors)
        {
            foreach (var comp in fleet.Value)
            {
                streamWriter.WriteLine(GetScratchSheetInfo(fleet.Key, comp));
            }
        }

        streamWriter.Flush();
        stream.Position = 0;

        return stream;
    }



    private string GetCompetitorHeaders()
    {
        var sb = new StringBuilder();
        sb.Append(GetEscapedLocalizedValue("Fleet"));
        sb.Append(_separator);
        sb.Append(GetEscapedLocalizedValue("Sail"));
        sb.Append(_separator);
        sb.Append(GetEscapedLocalizedValue("Alt Sail"));
        sb.Append(_separator);
        sb.Append(GetEscapedLocalizedValue("Sailor(s)"));
        sb.Append(_separator);
        sb.Append(GetEscapedLocalizedValue("Boat"));
        sb.Append(_separator);
        sb.Append(GetEscapedLocalizedValue("Club"));
        sb.Append(_separator);
        sb.Append(GetEscapedLocalizedValue("Class"));

        return sb.ToString();
    }

    private string GetScratchSheetInfo(String group, Competitor comp)
    {
        var sb = new StringBuilder();
        sb.Append(GetEscapedValue(group));
        sb.Append(_separator);
        sb.Append(GetEscapedValue(comp.SailNumber));
        sb.Append(_separator);
        sb.Append(GetEscapedValue(comp.AlternativeSailNumber));
        sb.Append(_separator);
        sb.Append(GetEscapedValue(comp.Name));
        sb.Append(_separator);
        sb.Append(GetEscapedValue(comp.BoatName));
        sb.Append(_separator);
        sb.Append(GetEscapedValue(comp.HomeClubName));
        sb.Append(_separator);
        sb.Append(GetEscapedValue(comp.BoatClass?.Name));

        return sb.ToString();
    }



    private string GetEscapedLocalizedValue(string s)
    {
        return GetEscapedValue(_stringLocalizer[s]);
    }

    private static string GetEscapedValue(string s)
    {
        if (String.IsNullOrWhiteSpace(s)) return string.Empty;
        var returnString = s.Replace(_quote, _quote + _quote);
        if (s.Contains(_separator))
        {
            return _quote + returnString + _quote;
        }
        else
        {
            return returnString;
        }

    }

    public void Dispose()
    {
        if (stream != null)
        {
            stream.Dispose();
        }
        stream = new MemoryStream();
        if (streamWriter != null)
        {
            streamWriter.Dispose();
        }
    }
}
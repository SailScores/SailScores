using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using SailScores.Api.Enumerations;
using SailScores.Core.Extensions;
using SailScores.Core.FlatModel;
using SailScores.Database;
using SailScores.Web.Models.SailScores;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace SailScores.Web.Resources;

public class LocalizerService : ILocalizerService
{
    private readonly IStringLocalizer _localizer;
    private readonly ISailScoresContext _dbContext;
    private readonly IMemoryCache _cache;

    private readonly string cacheKeyName = "ClubLocaleCache";

    private static bool _includePseudo;

    public LocalizerService(
        IStringLocalizerFactory factory,
        ISailScoresContext dbContext,
        IMemoryCache memoryCache)
    {
        var type = typeof(SharedResource);
        var assemblyName = new AssemblyName(type.GetTypeInfo().Assembly.FullName);
        _localizer = factory.Create("SharedResource", assemblyName.Name);
        _dbContext = dbContext;
        _cache = memoryCache;
    }

    public string this[string key] => _localizer[key];

    public string GetFullRaceName(RaceViewModel race)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(race.Name))
        {
            sb.Append(race.Name);
            sb.Append(' ');
        }

        var useParens = !string.IsNullOrWhiteSpace(race.Name) && race.Date.HasValue;
        if (useParens) sb.Append('(');

        switch (race.State)
        {
            case RaceState.Scheduled:
                sb.Append(_localizer["Scheduled for"]);
                sb.Append(' ');
                break;
            case RaceState.Abandoned:
                sb.Append(_localizer["Abandoned"]);
                sb.Append(". ");
                break;
        }

        if (race.Date.HasValue)
        {
            sb.Append(race.Date.Value.ToString("D", CultureInfo.CurrentCulture));
        }

        if (race.Order > 0 && race.State != RaceState.Scheduled)
        {
            sb.Append(' ');
            sb.Append(GetRaceLetter());
            sb.Append(race.Order);
        }

        if (useParens) sb.Append(')');

        return sb.ToString();
    }

    public string GetShortName(FlatRace race)
    {
        if (string.IsNullOrEmpty(race.Name))
        {
            var firstLetter = GetRaceLetter();
            return $"{race.Date.ToSuperShortString()} {firstLetter}{race.Order}";
        }
        else if ((race.IsSeries ?? false) && race.StartDate != null && race.EndDate != null)
        {
            return $"{race.Name} ({race.StartDate.ToSuperShortString()} - {race.EndDate.ToSuperShortString()})";
        }
        else
        {
            return $"{race.Name} ({race.Date.ToSuperShortString()})";
        }
    }

    public string GetRaceLetter()
    {
        var s = _localizer["Race"].ToString();
        var first = string.IsNullOrEmpty(s) ? "R" : s.Substring(0, 1);
        return CultureInfo.CurrentCulture.TextInfo.ToUpper(first);
    }

    public LocalizedString GetLocalizedHtmlString(string key) => _localizer[key];

    public async Task UpdateCulture(string initials, string locale)
    {
        var clubInitialsToLocales = await _dbContext.Clubs
            .ToDictionaryAsync(c => c.Initials.ToUpperInvariant(), c => c.Locale);

        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(300)
        };

        clubInitialsToLocales[initials.ToUpperInvariant()] = locale;
        _cache.Set(cacheKeyName, clubInitialsToLocales, cacheEntryOptions);
    }

    public Dictionary<string, string> SupportedLocalizations => _supportedLocalizations;

    public string DefaultLocalization => "en-US";

    private static Dictionary<string, string> _supportedLocalizations =
        new Dictionary<string, string> {
            { "en-AU", "English (Australia)" },
            //{ "en-CA", "English (Canada)" },
            //{ "en-DE", "English (Germany)" },
            //{ "en-GB", "English (Great Britain)" },
            { "en-IE", "English (Ireland)" },
            //{ "en-ZA", "English (South Africa)" },
            { "en-US", "English (United States)" },
            { "fi-FI", "Finnish (Finland)" },
            //{ "sr-Latn-RS", "Serbian Latin (Serbia)" },
            //{ "es-AR", "Spanish (Argentina)" },
            { "sv-FI", "Swedish (Finland)" },
            { "qps-ploc", "Pseudo-Localized" }
        };

    public static List<CultureInfo> GetSupportedCultures(bool includePseudo)
    {
        _includePseudo = includePseudo;
        var map = new Dictionary<string, string>(_supportedLocalizations);
        if (includePseudo)
        {
            map["qps-ploc"] = "Pseudo-Localized";
        }
        return map.Select(l => new CultureInfo(l.Key)).ToList();
    }
    public static List<CultureInfo> GetSupportedCultures()
    {
        return GetSupportedCultures(_includePseudo); 
    }
}

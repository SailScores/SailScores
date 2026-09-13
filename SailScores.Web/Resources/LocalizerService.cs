using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using SailScores.Core.Utility;
using SailScores.Api.Enumerations;
using SailScores.Core.Extensions;
using SailScores.Core.FlatModel;
using SailScores.Database;
using SailScores.Web.Models.SailScores;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace SailScores.Web.Resources;

public class LocalizerService : ILocalizerService
{
    private readonly IStringLocalizer _localizer;
    private readonly ISailScoresContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly string cacheKeyName = "ClubLocaleCache";

    private static bool _includePseudo;

    public LocalizerService(
        IStringLocalizerFactory factory,
        ISailScoresContext dbContext,
        IMemoryCache memoryCache,
        IHttpContextAccessor httpContextAccessor)
    {
        var type = typeof(SharedResource);
        var assemblyName = new AssemblyName(type.GetTypeInfo().Assembly.FullName);
        _localizer = factory.Create("SharedResource", assemblyName.Name);
        _dbContext = dbContext;
        _cache = memoryCache;
        _httpContextAccessor = httpContextAccessor;
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
        return GetShortName(race, null);
    }

    public string GetShortName(FlatRace race, string defaultDateFormat)
    {
        var effectiveDateFormat = ResolveEffectiveDateFormat(defaultDateFormat);
        if (string.IsNullOrEmpty(race.Name))
        {
            var firstLetter = GetRaceLetter();
            return $"{FormatRaceDate(race.Date, effectiveDateFormat)} {firstLetter}{race.Order}";
        }
        else if ((race.IsSeries ?? false) && race.StartDate != null && race.EndDate != null)
        {
            if (race.StartDate == race.EndDate)
            {
                return $"{race.Name} ({FormatRaceDate(race.StartDate, effectiveDateFormat)})";
            }
            else
            {
                var startDate = FormatRaceDate(race.StartDate, effectiveDateFormat);
                var endDate = FormatRaceDate(race.EndDate, effectiveDateFormat);
                return $"{race.Name} ({startDate} - {endDate})";
            }
        }
        else
        {
            return $"{race.Name} ({FormatRaceDate(race.Date, effectiveDateFormat)})";
        }
    }

    private string ResolveEffectiveDateFormat(string defaultDateFormat)
    {
        if (ClubDateFormatUtility.TryNormalize(defaultDateFormat, out var normalizedFormat, out _)
            && !string.IsNullOrWhiteSpace(normalizedFormat))
        {
            return normalizedFormat;
        }

        var path = _httpContextAccessor.HttpContext?.Request.Path ?? PathString.Empty;
        if (path == PathString.Empty)
        {
            return null;
        }

        var clubInitials = GetClubInitialsFromPath(path);
        if (string.IsNullOrWhiteSpace(clubInitials))
        {
            return null;
        }

        return GetClubDateFormat(clubInitials);
    }

    private string GetClubDateFormat(string clubInitials)
    {
        var dbFormat = _dbContext.Clubs
            .Where(c => c.Initials == clubInitials)
            .Select(c => c.DefaultDateFormat)
            .FirstOrDefault();

        return ClubDateFormatUtility.TryNormalize(dbFormat, out var normalizedFormat, out _)
            ? normalizedFormat
            : null;
    }

    private static string GetClubInitialsFromPath(PathString path)
    {
        var value = path.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parts = value.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        return parts[0];
    }

    private static string FormatRaceDate(DateTime? date, string dateFormat)
    {
        if (!string.IsNullOrWhiteSpace(dateFormat))
        {
            return date?.ToString(dateFormat, CultureInfo.CurrentCulture);
        }

        return date.ToSuperShortString();
    }

    private static string FormatRaceDate(DateOnly? date, string dateFormat)
    {
        if (!string.IsNullOrWhiteSpace(dateFormat))
        {
            return date?.ToString(dateFormat, CultureInfo.CurrentCulture);
        }

        return date.ToSuperShortString();
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

    public string DefaultLocalization => "en-US";

    private static Dictionary<string, string> _supportedLocalizations =
        new Dictionary<string, string> {
            { "en-AU", "English (Australia)" },
            { "en-CA", "English (Canada)" },
            { "en-DE", "English (Germany)" },
            { "en-GB", "English (Great Britain)" },
            { "en-IE", "English (Ireland)" },
            { "en-ZA", "English (South Africa)" },
            { "en-US", "English (United States)" },
            { "fi-FI", "Finnish (Finland)" },
            { "sr-Latn-RS", "Serbian Latin (Serbia)" },
            { "es-AR", "Spanish (Argentina)" },
            { "sv-FI", "Swedish (Finland)" },
        };

    public static Dictionary<string, string> GetSupportedLocalisations(bool includePseudo)
    {
        _includePseudo = includePseudo;
        var map = new Dictionary<string, string>(_supportedLocalizations);
        if (includePseudo)
        {
            map["qps-ploc"] = "Pseudo-Localized";
        }
        return map;
    }

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

    public string GetLocaleLongName(string locale)
    {
        var locales = GetAllLocales();
        if (string.IsNullOrWhiteSpace(locale))
        {
            return locales[DefaultLocalization];
        }
        var found = locales.TryGetValue(locale, out var longName);
        return found ? longName! : locales[DefaultLocalization];
    }

    public string GetLocaleShortName(string locale)
    {
        var allLocales = GetAllLocales();
        if (string.IsNullOrWhiteSpace(locale) || !allLocales.ContainsValue(locale)) {
        
            return DefaultLocalization;
        }

        return allLocales.First(l => l.Value == locale).Key;

    }
    private Dictionary<string, string> GetAllLocales()
    {
        var locales = new Dictionary<string, string>(_supportedLocalizations);
        if (!locales.ContainsKey("qps-ploc"))
        {
            locales["qps-ploc"] = "Pseudo-Localized";
        }
        return locales;
    }
}

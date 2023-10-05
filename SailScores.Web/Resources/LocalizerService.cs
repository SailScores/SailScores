using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using SailScores.Database;
using System.Reflection;

namespace SailScores.Web.Resources
{
    public class LocalizerService : ILocalizerService
    {
        private readonly IStringLocalizer _localizer;
        private readonly ISailScoresContext _dbContext;
        private readonly IMemoryCache _cache;

        private readonly string cacheKeyName = "ClubLocaleCache";

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

        public LocalizedString GetLocalizedHtmlString(string key)
        {
            return _localizer[key];
        }

        public async Task UpdateCulture(string initials, string locale)
        {
            var clubInitialsToLocales = _dbContext.Clubs
                        .ToDictionary(
                            c => c.Initials.ToUpperInvariant(),
                            c => c.Locale);
            var cacheEntryOptions = new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(300)
            };
            clubInitialsToLocales[initials.ToUpperInvariant()] = locale;

            // Save data in cache.
            _cache.Set(cacheKeyName, clubInitialsToLocales, cacheEntryOptions);
        }

        public Dictionary<string, string> SupportedLocalizations
        {
            get
            {
                return _supportedLocalizations;
            }
        }

        public string DefaultLocalization { get { return "en-US"; } }

        private Dictionary<string, string> _supportedLocalizations =
            new Dictionary<string, string> {
                { "en-AU", "English (Australia)" },
                { "en-IE", "English (Ireland)" },
                { "en-US", "English (United States)" },
                { "fi-FI", "Finnish (Finland)" },
                { "sv-FI", "Swedish (Finland)" },
            };
    }
}

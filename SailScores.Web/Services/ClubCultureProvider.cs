using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Caching.Memory;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace SailScores.Web.Services
{
    public class ClubCultureProvider : IRequestCultureProvider
    {
        private ISailScoresContext _dbContext;
        private IMemoryCache _cache;
        private readonly string cacheKeyName = "ClubLocaleCache";

        private string _defaultCultureString = "en-US";

        public async Task<ProviderCultureResult> DetermineProviderCultureResult(
            HttpContext httpContext)
        {
            _dbContext = httpContext.RequestServices.GetService<ISailScoresContext>();
            _cache = httpContext.RequestServices.GetService<IMemoryCache>();
            var locale = await GetLocaleAsync(httpContext.Request.Path).ConfigureAwait(false);
            if (String.IsNullOrWhiteSpace(locale))
            {
                return null;
            }
            return new ProviderCultureResult(locale);
        }


        public async Task<String> GetLocaleAsync(PathString path)
        {
            // cache all initials from the db.
            Dictionary<string, string> clubInitialsToLocales;

            if (!_cache.TryGetValue(cacheKeyName, out clubInitialsToLocales))
            {
                try
                {
                    clubInitialsToLocales = _dbContext.Clubs
                    .ToDictionary(
                        c => c.Initials.ToUpperInvariant(),
                        c => c.Locale);
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(300)
                    };

                    // Save data in cache.
                    _cache.Set(cacheKeyName, clubInitialsToLocales, cacheEntryOptions);
                }
                catch (InvalidOperationException)
                {
                    // swallowing: this is a likely error on initial load.
                }
            }

            if (clubInitialsToLocales == null) return _defaultCultureString;

            var clubCulture = clubInitialsToLocales
                .FirstOrDefault(kvp =>
                    path.StartsWithSegments($"/{kvp.Key}", StringComparison.InvariantCultureIgnoreCase));

            return clubCulture.Equals(default(KeyValuePair<string, string>))
                ? _defaultCultureString
                : clubCulture.Value;
        }
    }
}

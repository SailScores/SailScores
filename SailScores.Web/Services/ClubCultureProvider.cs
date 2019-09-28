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
        
        public async Task<ProviderCultureResult> DetermineProviderCultureResult(
            HttpContext httpContext)
        {
            _dbContext = httpContext.RequestServices.GetService<ISailScoresContext>();
            _cache = httpContext.RequestServices.GetService<IMemoryCache>();
            var locale = await GetLocaleAsync(httpContext.Request.Path);
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
                clubInitialsToLocales = _dbContext.Clubs
                .ToDictionary(
                    c => c.Initials.ToUpperInvariant(),
                    c => c.Locale);
                //TODO: need to test reloading the cache from db occasionally,
                //even if it is getting frequent use.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(60));

                // Save data in cache.
                _cache.Set(cacheKeyName, clubInitialsToLocales, cacheEntryOptions);
            }
            PathString remaining;
            foreach(var key in clubInitialsToLocales.Keys)
            {
                if (path.StartsWithSegments($"/{key}", StringComparison.InvariantCultureIgnoreCase))
                {
                    return clubInitialsToLocales[key];
                }
            }

            return "en-US";
        }
    }
}

﻿using Microsoft.AspNetCore.Http;
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
            var clubInitials = await GetClubInitials(httpContext);
            var locale = await GetClubLocale(clubInitials);
            if (String.IsNullOrWhiteSpace(locale))
            {
                return null;
            }
            return new ProviderCultureResult(locale);
        }

        private async Task<string> GetClubInitials(HttpContext httpContext)
        {
            //TODO
            return "LHYC";
        }

        public async Task<String> GetClubLocale(string clubInitials)
        {
            // cache all initials from the db.
            Dictionary<string, string> clubInitialsToLocales;
            
            if (!_cache.TryGetValue(cacheKeyName, out clubInitialsToLocales))
            {
                clubInitialsToLocales = _dbContext.Clubs
                .ToDictionary(
                    c => c.Initials,
                    c => c.Locale);
                //TODO: need to deal with reloading the cache from db occasionally,
                //even if it is getting frequent use.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(60));

                // Save data in cache.
                _cache.Set(cacheKeyName, clubInitialsToLocales, cacheEntryOptions);
            }

            return clubInitialsToLocales[clubInitials.ToUpperInvariant()];
        }
    }
}
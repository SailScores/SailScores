using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using SailScores.Database;

namespace SailScores.Web
{
    public class ClubRouteConstraint : IRouteConstraint
    {
        private readonly Func<ISailScoresContext> _createDbContext;
        private readonly IMemoryCache _cache;

        private const string cacheKeyName = "CachedClubList";

        public ClubRouteConstraint(
            Func<ISailScoresContext> createDbContext,
            IMemoryCache cache)
        {
            _createDbContext = createDbContext;
            _cache = cache;
        }

        /// <summary>
        /// Returns True if club initials are valid.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="route"></param>
        /// <param name="routeKey"></param>
        /// <param name="values"></param>
        /// <param name="routeDirection"></param>
        /// <returns></returns>
        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            // could rewrite to cache misses as well as hits. Right now, misses will always force a call to the DB.

            List<string> clubInitials;

            string potentialClubInitials = values[routeKey].ToString().ToUpperInvariant();

            if (!_cache.TryGetValue(cacheKeyName, out clubInitials) || !clubInitials.Contains(potentialClubInitials))
            {
                // Key not in cache, so get data.
                using (var context = _createDbContext())
                {
                    clubInitials = context.Clubs.Select(c => c.Initials.ToUpperInvariant()).ToList();
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(60));

                // Save data in cache.
                _cache.Set(cacheKeyName, clubInitials, cacheEntryOptions);
            }

            return clubInitials.Contains(potentialClubInitials);


        }
    }
}

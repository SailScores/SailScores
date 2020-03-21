using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public class AppSettingsService
    {
        private readonly IConfiguration _config;

        public AppSettingsService(
            IConfiguration config)
        {
            _config = config;
        }

        public string GetPreferredUri(HttpRequest request)
        {
            var preferredhost = _config["PreferredHost"];
            if (!String.IsNullOrWhiteSpace(preferredhost))
            {
                request.Host = new HostString(preferredhost);
            }

            var absoluteUri = string.Concat(
                        request.Scheme,
                        "://",
                        request.Host.ToUriComponent(),
                        request.PathBase.ToUriComponent(),
                        request.Path.ToUriComponent(),
                        request.QueryString.ToUriComponent());

            return absoluteUri;
        }
    }
}

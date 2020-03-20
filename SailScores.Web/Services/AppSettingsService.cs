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
            //var preferredBase = _config["baseUrl"];
            //if (String.IsNullOrWhiteSpace(preferredBase))
            //{
            //    return request.GetEncodedUrl();
            //}
            request.Host = new HostString("www.sailscores.com");
            //var newUri = (new Uri(preferredBase, request.Path.ToUriComponent())).ToString() + "?" + request.QueryString.ToUriComponent();
            //return request.GetEncodedUrl();
            return "~" + request.GetEncodedPathAndQuery();
        }

        public string Version =>
    Assembly.GetEntryAssembly()
    .GetName().Version.ToString();

        public string InformationalVersion =>
            Assembly.GetEntryAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            .InformationalVersion;

    }
}

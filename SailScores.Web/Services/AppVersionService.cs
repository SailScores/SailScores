using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public class AppVersionService : IAppVersionService
    {
        public string Version =>
            Assembly.GetEntryAssembly()
            .GetName().Version.ToString();

        public string InformationalVersion =>
            Assembly.GetEntryAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            .InformationalVersion;

    }
}

using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Localization;
using System.Reflection;

namespace SailScores.Web.Resources
{
    public class LocalizerService : ILocalizerService
    {
        private readonly IStringLocalizer _localizer;

        public LocalizerService(IStringLocalizerFactory factory)
        {
            var type = typeof(SharedResource);
            var assemblyName = new AssemblyName(type.GetTypeInfo().Assembly.FullName);
            _localizer = factory.Create("SharedResource", assemblyName.Name);
        }

        public LocalizedString GetLocalizedHtmlString(string key)
        {
            return _localizer[key];
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

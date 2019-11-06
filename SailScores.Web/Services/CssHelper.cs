using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public static class CssHelper
    {
        public static IHtmlContent EmbedCss(this IHtmlHelper htmlHelper, string path)
        {
            var env = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IHostingEnvironment>();

            // take a path that starts with "~" and map it to the filesystem.
            var cssFilePath = System.IO.Path.Combine(env.WebRootPath, path);
            // load the contents of that file
            try
            {
                var cssText = File.ReadAllText(cssFilePath);
                return htmlHelper.Raw("<style>\n" + cssText + "\n</style>");
            }
            catch
            {
                // return nothing if we can't read the file for any reason
                return null;
            }
        }
    }
}

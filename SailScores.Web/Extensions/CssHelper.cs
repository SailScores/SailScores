using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace SailScores.Web.Extensions
{
    public static class CssHelper
    {
        public static async Task<IHtmlContent> EmbedCss(this IHtmlHelper htmlHelper, string path)
        {
            var env = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IHostingEnvironment>();

            // take a path that starts with "~" and map it to the filesystem.
            var cssFilePath = System.IO.Path.Combine(env.WebRootPath, path);
            // load the contents of that file
            try
            {
                var cssText = await File.ReadAllTextAsync(cssFilePath);
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

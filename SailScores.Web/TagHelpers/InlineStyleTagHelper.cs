using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace SailScores.Web.TagHelpers;

public class InlineStyleTagHelper : TagHelper
{
    public InlineStyleTagHelper(IHostingEnvironment hostingEnvironment, IMemoryCache cache)
    {
        HostingEnvironment = hostingEnvironment;
        Cache = cache;
    }

    [HtmlAttributeName("href")]
    public string Href { get; set; }

    private IHostingEnvironment HostingEnvironment { get; }
    private IMemoryCache Cache { get; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var path = Href;

        // Get the value from the cache, or compute the value and add it to the cache
        var fileContent = await Cache.GetOrCreateAsync("InlineStyleTagHelper-" + path, async entry =>
        {
            IFileProvider fileProvider = HostingEnvironment.WebRootFileProvider;
            IChangeToken changeToken = fileProvider.Watch(path);

            entry.SetPriority(CacheItemPriority.NeverRemove);
            entry.AddExpirationToken(changeToken);

            IFileInfo file = fileProvider.GetFileInfo(path);
            if (file == null || !file.Exists)
                return null;

            return await ReadFileContent(file);
        });

        if (fileContent == null)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "style";
        output.Attributes.RemoveAll("href");
        output.Content.AppendHtml(fileContent);
    }

    private static async Task<string> ReadFileContent(IFileInfo file)
    {
        using (var stream = file.CreateReadStream())
        using (var textReader = new StreamReader(stream))
        {
            return await textReader.ReadToEndAsync();
        }
    }
}
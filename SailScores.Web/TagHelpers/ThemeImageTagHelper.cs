using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace SailScores.Web.TagHelpers;

[HtmlTargetElement("theme-image", Attributes = SrcAttributeName)]
public class ThemeImageTagHelper : TagHelper
{
    private const string SrcAttributeName = "src";

    /// <summary>
    /// Path to the default (light) image, e.g. "/images/help/confirm-club-settings.png"
    /// </summary>
    [HtmlAttributeName(SrcAttributeName)]
    public string Src { get; set; }

    /// <summary>
    /// Alternate path for dark mode. If not provided the DarkSuffix is inserted before the extension.
    /// </summary>
    [HtmlAttributeName("dark-src")]
    public string DarkSrc { get; set; }

    /// <summary>
    /// Suffix inserted before the extension to derive dark filename when DarkSrc not set. Default "-dark".
    /// </summary>
    [HtmlAttributeName("dark-suffix")]
    public string DarkSuffix { get; set; } = "-dark";

    [HtmlAttributeName("alt")]
    public string Alt { get; set; }

    [HtmlAttributeName("class")]
    public string Class { get; set; }

    // Allow passthrough for attributes like asp-append-version
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Src))
        {
            output.SuppressOutput();
            return;
        }

        var dark = string.IsNullOrWhiteSpace(DarkSrc) ? InsertSuffix(Src, DarkSuffix) : DarkSrc;

        output.TagName = "picture";
        output.TagMode = TagMode.StartTagAndEndTag;

        // Build inner HTML
        var sb = new System.Text.StringBuilder();
        sb.Append("<source srcset=\"").Append(HtmlEncoder.Default.Encode(dark)).Append("\" media=\"(prefers-color-scheme: dark)\">\n");
        sb.Append("<source srcset=\"").Append(HtmlEncoder.Default.Encode(Src)).Append("\" media=\"(prefers-color-scheme: light)\">\n");

        sb.Append("<img src=\"").Append(HtmlEncoder.Default.Encode(Src)).Append("\"");

        if (!string.IsNullOrWhiteSpace(Alt))
        {
            sb.Append(" alt=\"").Append(HtmlEncoder.Default.Encode(Alt)).Append("\"");
        }

        if (!string.IsNullOrWhiteSpace(Class))
        {
            sb.Append(" class=\"").Append(HtmlEncoder.Default.Encode(Class)).Append("\"");
        }

        // copy passthrough attributes (like asp-append-version) from context
        foreach (var attribute in context.AllAttributes)
        {
            var name = attribute.Name;
            if (name == "src" || name == "alt" || name == "class" || name == "dark-src" || name == "dark-suffix")
                continue;

            // render attribute as-is
            sb.Append(' ').Append(name).Append("=\"").Append(HtmlEncoder.Default.Encode(attribute.Value?.ToString() ?? "")).Append("\"");
        }

        sb.Append(" />");

        output.Content.SetHtmlContent(sb.ToString());
    }

    private static string InsertSuffix(string path, string suffix)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        var idx = path.LastIndexOf('.');
        if (idx <= 0)
            return path + suffix;

        return path.Substring(0, idx) + suffix + path.Substring(idx);
    }
}

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using SailScores.Web.Extensions;
using System.Threading.Tasks;

namespace SailScores.Web.TagHelpers
{
    // from https://stackoverflow.com/questions/50728261/

    [HtmlTargetElement("label", Attributes = ForAttributeName)]
    public class LabelRequiredTagHelper : LabelTagHelper
    {
        private const string ForAttributeName = "asp-for";
        private const string RequiredCssClass = "required";

        public LabelRequiredTagHelper(IHtmlGenerator generator) : base(generator)
        {
        }
        public override async Task ProcessAsync(
            TagHelperContext context,
            TagHelperOutput output)
        {
            await base.ProcessAsync(context, output);

            if (For.Metadata.IsRequired)
            {
                output.Attributes.AddCssClass(RequiredCssClass);
            }
        }
    }
}

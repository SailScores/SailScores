using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SailScores.Web.Extensions
{
    public static class TagHelperAttributeListExtensions
    {
        public static void AddCssClass(this TagHelperAttributeList attributeList,
            string cssClass)
        {
            var existingCssClassValue = attributeList
                .FirstOrDefault(x => x.Name == "class")?.Value.ToString();

            // If the class attribute doesn't exist, or the class attribute
            // value is empty, just add the CSS class
            if (String.IsNullOrEmpty(existingCssClassValue))
            {
                attributeList.SetAttribute("class", cssClass);
            }
            // Here I use Regular Expression to check if the existing css class
            // value has the css class already. If yes, you don't need to add
            // that css class again. Otherwise you just add the css class along
            // with the existing value.
            // \b indicates a word boundary, as you only want to check if
            // the css class exists as a whole word.  
            else if (!Regex.IsMatch(existingCssClassValue, $@"\b{ cssClass }\b",
                RegexOptions.IgnoreCase))
            {
                attributeList.SetAttribute("class", $"{ cssClass } { existingCssClassValue }");
            }
        }
    }
}

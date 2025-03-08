using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SailScores.Core.Services
{
    public static class UrlUtility
    {

        public static string GetUrlName(string rawName)
        {
            if (String.IsNullOrWhiteSpace(rawName))
            {
                return null;
            }

            // Normalize the string to decompose characters
            string normalizedString = rawName.Normalize(NormalizationForm.FormD);

            // Remove diacritics
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            // Convert to lowercase
            string result = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

            // Replace spaces with hyphens
            result = Regex.Replace(result, @"\s+", "-");

            // Remove invalid URL characters
            result = Regex.Replace(result, @"[^a-z0-9\-]", "");

            // Remove multiple hyphens
            result = Regex.Replace(result, @"-+", "-").Trim('-');

            return result;
        }

    }
}

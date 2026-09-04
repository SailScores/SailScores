using SailScores.Core.Utility;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ClubDateFormatAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is not string format)
        {
            return ValidationResult.Success;
        }

        return ClubDateFormatUtility.TryNormalize(format, out _, out var error)
            ? ValidationResult.Success
            : new ValidationResult(error);
    }
}

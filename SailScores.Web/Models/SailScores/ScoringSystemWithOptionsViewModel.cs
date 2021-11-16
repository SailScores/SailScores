using SailScores.Core.Model;
using SailScores.Core.Services;
using DataAnnotations = System.ComponentModel.DataAnnotations;
using System.Text;

namespace SailScores.Web.Models.SailScores;

public class ScoringSystemWithOptionsViewModel : ScoringSystem , DataAnnotations.IValidatableObject
{
    public IList<ScoringSystem> ParentSystemOptions { get; set; }
    public IList<ScoreCode> ScoreCodeOptions { get; set; }

    public IEnumerable<DataAnnotations.ValidationResult> Validate(DataAnnotations.ValidationContext validationContext)
    {
        var discardSequenceErrors = ScoringService.GetDiscardSequenceErrors(this.DiscardPattern);
        var errorString = new StringBuilder();
        foreach(var error in discardSequenceErrors)
        {

            if (errorString.Length > 0)
            {
                errorString.Append("; ");
            }
            errorString.Append(error);
        }
        if (errorString.Length > 0)
        {
            yield return new DataAnnotations.ValidationResult(
                $"Discard pattern is not valid: {errorString}",
                new[] { nameof(DiscardPattern) });
        }
    }
}
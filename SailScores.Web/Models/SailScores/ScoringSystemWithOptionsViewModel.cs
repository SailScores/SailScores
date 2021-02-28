using SailScores.Core.Model;
using SailScores.Core.Services;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SailScores.Web.Models.SailScores
{
    public class ScoringSystemWithOptionsViewModel : ScoringSystem , IValidatableObject
    {
        public IList<ScoringSystem> ParentSystemOptions { get; set; }
        public IList<ScoreCode> ScoreCodeOptions { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
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
                yield return new ValidationResult(
                    $"Discard pattern is not valid: {errorString}",
                    new[] { nameof(DiscardPattern) });
            }
        }
    }
}

namespace SailScores.Web.Models.SailScores;

public class ClubCollectionViewModel<T> : ClubBaseViewModel
{
    public IEnumerable<T> List { get; set; }
}
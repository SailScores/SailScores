namespace SailScores.Web.Models.SailScores
{
    public class ClubItemViewModel<T> : ClubBaseViewModel
    {
        public T Item { get; set; }
    }
}

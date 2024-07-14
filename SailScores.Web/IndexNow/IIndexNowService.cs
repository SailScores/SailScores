namespace SailScores.Web.IndexNow;

public interface IIndexNowSubmitter
{
    Task SubmitUrls(IList<string> urls);
}
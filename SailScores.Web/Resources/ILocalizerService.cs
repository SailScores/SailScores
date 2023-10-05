
namespace SailScores.Web.Resources
{
    public interface ILocalizerService
    {
        public Dictionary<string, string> SupportedLocalizations { get; }
        string DefaultLocalization { get; }

        Task UpdateCulture(string initials, string locale);
    }
}
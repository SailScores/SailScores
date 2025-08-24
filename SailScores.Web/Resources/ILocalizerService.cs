
using SailScores.Core.FlatModel;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Resources
{
    public interface ILocalizerService
    {

        string this[string key] { get; }

        public Dictionary<string, string> SupportedLocalizations { get; }
        public string DefaultLocalization { get; }

        public string GetShortName(FlatRace race);

        public string GetFullRaceName(RaceViewModel race);

        Task UpdateCulture(string initials, string locale);
    }
}
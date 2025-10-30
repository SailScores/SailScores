
using SailScores.Core.FlatModel;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Resources
{
    public interface ILocalizerService
    {

        string this[string key] { get; }

        public string DefaultLocalization { get; }

        public string GetShortName(FlatRace race);

        public string GetFullRaceName(RaceViewModel race);

        Task UpdateCulture(string initials, string locale);
        string GetLocaleLongName(string locale);
        string GetLocaleShortName(string locale);
    }
}
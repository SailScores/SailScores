using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface ISeasonService
{
    Task SaveNew(Season season);
    Task Update(Season season);
    Task Delete(Guid seasonId);
    Task<IList<Season>> GetSeasons(Guid clubId);
        
    Task<IList<String>> GetSavingSeasonErrors(Season model);

    Task<SeasonWithOptionsViewModel> GetSeasonSuggestion(Guid clubId);
    Task<SeasonWithOptionsViewModel> GetSeasonForEdit(Guid clubId, Guid seasonId);
}

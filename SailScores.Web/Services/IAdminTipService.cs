using SailScores.Web.Models.SailScores;
using System.Collections.Generic;

namespace SailScores.Web.Services
{
    public interface IAdminTipService
    {
        void AddTips(ref AdminViewModel viewModel);
        void AddTips(ref RaceWithOptionsViewModel race);
        IList<AdminToDoViewModel> GetRaceCreateErrors(RaceWithOptionsViewModel race);
        IList<AdminToDoViewModel> GetSeriesCreateErrors(SeriesWithOptionsViewModel series);
        IList<AdminToDoViewModel> GetCompetitorCreateErrors(
            CompetitorWithOptionsViewModel competitor);
        IList<AdminToDoViewModel> GetMultipleCompetitorsCreateErrors(MultipleCompetitorsWithOptionsViewModel vm);
    }
}
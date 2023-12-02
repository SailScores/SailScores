using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IWhatIfService
{
    Task<WhatIfResultsViewModel> GetResults(WhatIfViewModel options);
    Task<IList<ScoringSystem>> GetScoringSystemOptions(Guid clubId);
}
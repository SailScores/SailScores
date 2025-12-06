using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces
{
    public interface ISupporterService
    {
        Task<IEnumerable<SupporterViewModel>> GetVisibleSupportersAsync();
        Task<IEnumerable<SupporterViewModel>> GetAllSupportersAsync();
        Task<SupporterWithOptionsViewModel> GetSupporterAsync(Guid id);
        Task<SupporterWithOptionsViewModel> GetBlankSupporter();
        Task SaveNew(SupporterWithOptionsViewModel supporter);
        Task Update(SupporterWithOptionsViewModel supporter);
        Task Delete(Guid id);
        Task<FileStreamResult> GetLogoAsync(Guid id);
    }
}

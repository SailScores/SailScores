using AutoMapper;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SailScores.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SailScores.Web.Services
{
    public class SupporterService : ISupporterService
    {
        private readonly Core.Services.ISupporterService _coreSupporterService;
        private readonly IMapper _mapper;
        private readonly ISailScoresContext _dbContext;
        private readonly Core.Services.IClubService _clubService;

        public SupporterService(
            Core.Services.ISupporterService coreSupporterService,
            IMapper mapper,
            ISailScoresContext dbContext,
            Core.Services.IClubService clubService)
        {
            _coreSupporterService = coreSupporterService;
            _mapper = mapper;
            _dbContext = dbContext;
            _clubService = clubService;
        }

        public async Task<IEnumerable<SupporterViewModel>> GetVisibleSupportersAsync()
        {
            var supporters = await _coreSupporterService.GetVisibleSupportersAsync();
            return await EnrichSupportersWithClubData(supporters);
        }

        public async Task<IEnumerable<SupporterViewModel>> GetAllSupportersAsync()
        {
            var supporters = await _coreSupporterService.GetAllSupportersAsync();
            return await EnrichSupportersWithClubData(supporters);
        }

        private async Task<IEnumerable<SupporterViewModel>> EnrichSupportersWithClubData(IEnumerable<Core.Model.Supporter> supporters)
        {
            var viewModels = _mapper.Map<IEnumerable<SupporterViewModel>>(supporters).ToList();
            
            // Get club data for supporters that are linked to clubs
            var clubIds = viewModels.Where(s => s.ClubId.HasValue).Select(s => s.ClubId.Value).Distinct().ToList();
            
            if (clubIds.Any())
            {
                var clubs = await _dbContext.Clubs
                    .Where(c => clubIds.Contains(c.Id))
                    .Select(c => new { c.Id, c.LogoFileId })
                    .ToListAsync();
                
                foreach (var supporter in viewModels.Where(s => s.ClubId.HasValue))
                {
                    var club = clubs.FirstOrDefault(c => c.Id == supporter.ClubId.Value);
                    if (club?.LogoFileId.HasValue == true && !supporter.LogoFileId.HasValue)
                    {
                        // Use club's burgee if supporter doesn't have its own logo
                        supporter.LogoFileId = club.LogoFileId;
                    }
                }
            }
            
            return viewModels;
        }

        public async Task<SupporterWithOptionsViewModel> GetSupporterAsync(Guid id)
        {
            var supporter = await _coreSupporterService.GetSupporterAsync(id);
            var vm = _mapper.Map<SupporterWithOptionsViewModel>(supporter);
            vm.ClubOptions = await GetClubOptionsAsync();
            return vm;
        }

        public async Task<SupporterWithOptionsViewModel> GetBlankSupporter()
        {
            return new SupporterWithOptionsViewModel
            {
                IsVisible = true,
                ClubOptions = await GetClubOptionsAsync()
            };
        }

        private async Task<List<ClubOption>> GetClubOptionsAsync()
        {
            var clubs = await _clubService.GetClubs(includeHidden: true);
            return clubs
                .OrderBy(c => c.Name)
                .Select(c => new ClubOption
                {
                    Id = c.Id,
                    Name = c.Name,
                    Initials = c.Initials,
                    LogoFileId = c.LogoFileId
                })
                .ToList();
        }

        public async Task SaveNew(SupporterWithOptionsViewModel supporter)
        {
            await ProcessLogoFile(supporter);
            var coreSupporter = _mapper.Map<Core.Model.Supporter>(supporter);
            await _coreSupporterService.SaveNewSupporter(coreSupporter);
        }

        public async Task Update(SupporterWithOptionsViewModel supporter)
        {
            await ProcessLogoFile(supporter);
            var coreSupporter = _mapper.Map<Core.Model.Supporter>(supporter);
            await _coreSupporterService.UpdateSupporter(coreSupporter);
        }

        public async Task Delete(Guid id)
        {
            await _coreSupporterService.DeleteSupporter(id);
        }

        private async Task ProcessLogoFile(SupporterWithOptionsViewModel supporter)
        {
            if (supporter.LogoFile != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await supporter.LogoFile.CopyToAsync(memoryStream);

                    // Upload the file if less than 2 MB
                    if (memoryStream.Length < 2097152)
                    {
                        var file = new Database.Entities.File
                        {
                            Id = Guid.NewGuid(),
                            FileContents = memoryStream.ToArray(),
                            Created = DateTime.UtcNow
                        };

                        await _dbContext.Files.AddAsync(file);
                        await _dbContext.SaveChangesAsync();

                        supporter.LogoFileId = file.Id;
                    }
                    else
                    {
                        throw new ArgumentException("File is too large. Maximum size is 2 MB.");
                    }
                }
            }
        }

        public async Task<FileStreamResult> GetLogoAsync(Guid id)
        {
            var file = await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == id);
            if (file == null)
            {
                return null;
            }
            var stream = new MemoryStream();
            await stream.WriteAsync(file.FileContents, 0, file.FileContents.Length);
            stream.Position = 0;
            return new FileStreamResult(stream, "image/png");
        }
    }
}

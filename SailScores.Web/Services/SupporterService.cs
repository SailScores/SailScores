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

        public SupporterService(
            Core.Services.ISupporterService coreSupporterService,
            IMapper mapper,
            ISailScoresContext dbContext)
        {
            _coreSupporterService = coreSupporterService;
            _mapper = mapper;
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<SupporterViewModel>> GetVisibleSupportersAsync()
        {
            var supporters = await _coreSupporterService.GetVisibleSupportersAsync();
            return _mapper.Map<IEnumerable<SupporterViewModel>>(supporters);
        }

        public async Task<IEnumerable<SupporterViewModel>> GetAllSupportersAsync()
        {
            var supporters = await _coreSupporterService.GetAllSupportersAsync();
            return _mapper.Map<IEnumerable<SupporterViewModel>>(supporters);
        }

        public async Task<SupporterWithOptionsViewModel> GetSupporterAsync(Guid id)
        {
            var supporter = await _coreSupporterService.GetSupporterAsync(id);
            return _mapper.Map<SupporterWithOptionsViewModel>(supporter);
        }

        public Task<SupporterWithOptionsViewModel> GetBlankSupporter()
        {
            return Task.FromResult(new SupporterWithOptionsViewModel
            {
                IsVisible = true
            });
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
            stream.Write(file.FileContents, 0, file.FileContents.Length);
            stream.Position = 0;
            return new FileStreamResult(stream, "image/png");
        }
    }
}

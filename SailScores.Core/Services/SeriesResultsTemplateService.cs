using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Services.Interfaces;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services;

public class SeriesResultsTemplateService : ISeriesResultsTemplateService
{
    private readonly ISailScoresContext _dbContext;
    private readonly IMapper _mapper;

    public SeriesResultsTemplateService(
        ISailScoresContext dbContext,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SeriesResultsTemplate>> GetTemplatesForClubAsync(Guid clubId)
    {
        var dbTemplates = await _dbContext.SeriesResultsTemplates
            .Where(t => t.ClubId == clubId)
            .OrderBy(t => t.Name)
            .ToListAsync()
            .ConfigureAwait(false);

        return _mapper.Map<List<SeriesResultsTemplate>>(dbTemplates);
    }

    public async Task<SeriesResultsTemplate> GetTemplateAsync(Guid templateId)
    {
        var dbTemplate = await _dbContext.SeriesResultsTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId)
            .ConfigureAwait(false);

        return _mapper.Map<SeriesResultsTemplate>(dbTemplate);
    }

    public async Task<SeriesResultsTemplate> SaveTemplateAsync(SeriesResultsTemplate template)
    {
        if (template.Id == Guid.Empty)
        {
            // Create new template
            var dbTemplate = _mapper.Map<Db.SeriesResultsTemplate>(template);
            dbTemplate.Id = Guid.NewGuid();
            _dbContext.SeriesResultsTemplates.Add(dbTemplate);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return _mapper.Map<SeriesResultsTemplate>(dbTemplate);
        }
        else
        {
            // Update existing template
            var dbTemplate = await _dbContext.SeriesResultsTemplates
                .FirstOrDefaultAsync(t => t.Id == template.Id)
                .ConfigureAwait(false);

            if (dbTemplate == null)
            {
                throw new InvalidOperationException("Template not found.");
            }

            _mapper.Map(template, dbTemplate);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return _mapper.Map<SeriesResultsTemplate>(dbTemplate);
        }
    }

    public async Task DeleteTemplateAsync(Guid templateId)
    {
        // Check if template is set as a club default
        var isDefault = await _dbContext.Clubs
            .AnyAsync(c => c.DefaultSeriesResultsTemplateId == templateId
                        || c.DefaultRegattaSeriesResultsTemplateId == templateId)
            .ConfigureAwait(false);

        if (isDefault)
        {
            throw new InvalidOperationException(
                "Cannot delete this template because it is set as a club default. " +
                "Please change the club default first.");
        }

        var dbTemplate = await _dbContext.SeriesResultsTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId)
            .ConfigureAwait(false);

        if (dbTemplate != null)
        {
            _dbContext.SeriesResultsTemplates.Remove(dbTemplate);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public async Task SeedDefaultTemplatesAsync(Guid clubId)
    {
        // Check if club already has default templates set
        var club = await _dbContext.Clubs
            .FirstOrDefaultAsync(c => c.Id == clubId)
            .ConfigureAwait(false);

        if (club == null)
        {
            throw new InvalidOperationException("Club not found.");
        }

        // Only seed if club doesn't already have templates
        var existingCount = await _dbContext.SeriesResultsTemplates
            .CountAsync(t => t.ClubId == clubId)
            .ConfigureAwait(false);

        if (existingCount > 0)
        {
            return; // Already has templates
        }

        // Create Standard template
        var standardTemplate = new Db.SeriesResultsTemplate
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Standard",
            SailNumberVisibility = ColumnVisibility.Always,
            CompetitorNameVisibility = ColumnVisibility.Always,
            CompetitorNameHeader = "Helm",
            BoatNameVisibility = ColumnVisibility.OnLargerScreens,
            BoatNameHeader = "Boat",
            CompetitorClubVisibility = ColumnVisibility.Hidden
        };

        // Create Regatta template
        var regattaTemplate = new Db.SeriesResultsTemplate
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Regatta",
            SailNumberVisibility = ColumnVisibility.Always,
            CompetitorNameVisibility = ColumnVisibility.Always,
            CompetitorNameHeader = "Helm",
            BoatNameVisibility = ColumnVisibility.OnLargerScreens,
            BoatNameHeader = "Boat",
            CompetitorClubVisibility = ColumnVisibility.OnLargerScreens
        };

        _dbContext.SeriesResultsTemplates.Add(standardTemplate);
        _dbContext.SeriesResultsTemplates.Add(regattaTemplate);

        // Set club defaults
        club.DefaultSeriesResultsTemplateId = standardTemplate.Id;
        club.DefaultRegattaSeriesResultsTemplateId = regattaTemplate.Id;

        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task EnsureDefaultTemplatesForAllClubsAsync()
    {
        var clubsWithoutDefaults = await _dbContext.Clubs
            .Where(c => c.DefaultSeriesResultsTemplateId == null
                     || c.DefaultRegattaSeriesResultsTemplateId == null)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var club in clubsWithoutDefaults)
        {
            await SeedDefaultTemplatesAsync(club.Id).ConfigureAwait(false);
        }
    }
}

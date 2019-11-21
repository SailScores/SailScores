using AutoMapper;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core = SailScores.Core;

namespace SailScores.Web.Services
{
    public class MergeService : IMergeService
    {
        private readonly Core.Services.IMergeService _coreMergeService;

        private readonly IMapper _mapper;

        public MergeService(
            Core.Services.IMergeService mergeService,
            IMapper mapper)
        {
            _coreMergeService = mergeService;
            _mapper = mapper;
        }

        public async Task<int?> GetNumberOfRaces(Guid? competitorId)
        {
            if (competitorId == null)
            {
                return null;
            }
            return await _coreMergeService.GetRaceCountFor(competitorId.Value);
        }

        public async Task<IList<Season>> GetSeasons(Guid? competitorId)
        {
            if(competitorId == null)
            {
                return new List<Season>();
            }
            return await _coreMergeService.GetSeasonsFor(competitorId.Value);
        }

        public async Task<IList<Competitor>> GetSourceOptionsFor(Guid? targetCompetitorId)
        {
            if(targetCompetitorId == null)
            {
                return new List<Competitor>();
            }

            return await _coreMergeService.GetSourceOptionsFor(targetCompetitorId.Value);
        }

        public async Task Merge(Guid? targetCompetitorId, Guid? sourceCompetitorId)
        {
            if(targetCompetitorId == null || sourceCompetitorId == null)
            {
                throw new ArgumentNullException("Missing a competitor id for merge");
            }

            await _coreMergeService.Merge(targetCompetitorId.Value, sourceCompetitorId.Value);
        }
    }
}

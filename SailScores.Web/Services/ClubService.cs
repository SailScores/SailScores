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
    public class ClubService : IClubService
    {
        private readonly Core.Services.IClubService _coreClubService;
        private readonly Core.Services.ISeasonService _coreSeasonService;
        private readonly Core.Services.IRaceService _coreRaceService;
        private readonly Core.Services.ISeriesService _coreSeriesService;

        private readonly IMapper _mapper;

        public ClubService(
            Core.Services.IClubService clubService,
            Core.Services.ISeasonService seasonService,
            Core.Services.IRaceService raceService,
            Core.Services.ISeriesService seriesService,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _coreSeasonService = seasonService;
            _coreRaceService = raceService;
            _coreSeriesService = seriesService;
            _mapper = mapper;
        }

        public async Task<Club> GetFullClub(string clubInitials)
        {
            return await _coreClubService.GetFullClub(clubInitials);
        }

        public async Task<Club> GetClubForClubHome(string clubInitials)
        {
            var clubId = await _coreClubService.GetClubId(clubInitials);
            var club = await _coreClubService.GetMinimalClub(clubId);
            club.Seasons = await _coreSeasonService.GetSeasons(clubId);
            club.Races = await _coreRaceService.GetRacesAsync(clubId);
            club.Series = await _coreSeriesService.GetAllSeriesAsync(clubId, null);
            return club;
        }
    }
}

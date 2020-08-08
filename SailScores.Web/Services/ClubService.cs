using SailScores.Core.Model;
using Entities = SailScores.Database.Entities;
using SailScores.Web.Models.SailScores;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace SailScores.Web.Services
{
    public class ClubService : IClubService
    {
        private readonly Core.Services.IClubService _coreClubService;
        private readonly Core.Services.ISeasonService _coreSeasonService;
        private readonly Core.Services.IRaceService _coreRaceService;
        private readonly Core.Services.ISeriesService _coreSeriesService;
        private readonly Core.Services.IRegattaService _coreRegattaService;
        private readonly IMapper _mapper;

        public ClubService(
            Core.Services.IClubService clubService,
            Core.Services.ISeasonService seasonService,
            Core.Services.IRaceService raceService,
            Core.Services.ISeriesService seriesService,
            Core.Services.IRegattaService regattaService,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _coreSeasonService = seasonService;
            _coreRaceService = raceService;
            _coreSeriesService = seriesService;
            _coreRegattaService = regattaService;
            _mapper = mapper;
        }

        public async Task<Club> GetClubForClubHome(string clubInitials)
        {
            var clubId = await _coreClubService.GetClubId(clubInitials);
            var club = await _coreClubService.GetMinimalClub(clubId);
            club.Seasons = await _coreSeasonService.GetSeasons(clubId);
            club.Races = await _coreRaceService.GetRacesAsync(clubId);
            club.Series = await _coreSeriesService.GetAllSeriesAsync(clubId, null, false);
            club.Regattas = await _coreRegattaService.GetAllRegattasAsync(clubId);
            return club;
        }

        public async Task<ClubStatsViewModel> GetClubStats(string clubInitials)
        {
            var seasonStats = await _coreClubService.GetClubStats(clubInitials);
            var firstSeason = seasonStats.FirstOrDefault();

            ClubStatsViewModel returnObj;
            if (seasonStats != null && firstSeason != null)
            {
                returnObj = new ClubStatsViewModel
                {
                    Initials = firstSeason.ClubInitials,
                    Name = firstSeason.ClubName,
                    SeasonStats = _mapper.Map<IEnumerable<ClubSeasonStatsViewModel>>(seasonStats)
                };
            } else
            {
                Club club = await _coreClubService.GetMinimalClub(clubInitials);
                returnObj = new ClubStatsViewModel
                {
                    Initials = club.Initials,
                    Name = club.Name,
                    SeasonStats = new List<ClubSeasonStatsViewModel>()
                };
            }
            return returnObj;
        }
    }
}

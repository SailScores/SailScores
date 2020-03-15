using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;

namespace SailScores.Web.Controllers
{
    [Authorize]
    public class MergeCompetitorController : Controller
    {
        private readonly Core.Services.IClubService _clubService;
        private readonly Web.Services.ICompetitorService _competitorService;
        private readonly IMapper _mapper;
        private readonly Services.IAuthorizationService _authService;
        private readonly Services.IMergeService _mergeService;

        public MergeCompetitorController(
            Core.Services.IClubService clubService,
            Web.Services.ICompetitorService competitorService,
            Services.IAuthorizationService authService,
            Services.IMergeService mergeService,
            IMapper mapper)
        {
            _clubService = clubService;
            _competitorService = competitorService;
            _authService = authService;
            _mergeService = mergeService;
            _mapper = mapper;
        }

        public async Task<ActionResult> Options(string clubInitials)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id))
            {
                return Unauthorized();
            }
            var vm = new MergeCompetitorViewModel
            {
                TargetCompetitorOptions = club.Competitors.OrderBy(c => c.Name).ToList()
            };
            return View("SelectTarget", vm);
        }

        [HttpPost]
        public async Task<ActionResult> Options(
            string clubInitials, 
            MergeCompetitorViewModel vm)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id))
            {
                return Unauthorized();
            }
            
            vm.SourceCompetitorOptions = await _mergeService.GetSourceOptionsFor(vm.TargetCompetitorId);
            return View("SelectSource", vm);
        }

        [HttpPost]
        public async Task<ActionResult> Verify(
            string clubInitials,
            MergeCompetitorViewModel vm)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id))
            {
                return Unauthorized();
            }

            vm.SourceCompetitor = await _competitorService.GetCompetitorAsync(vm.SourceCompetitorId.Value);
            vm.TargetCompetitor = await _competitorService.GetCompetitorAsync(vm.TargetCompetitorId.Value);
            if(vm.SourceCompetitor.ClubId != club.Id ||
                vm.TargetCompetitor.ClubId != club.Id)
            {
                return Unauthorized();
            }
            vm.SourceNumberOfRaces = await _mergeService.GetNumberOfRaces(vm.SourceCompetitorId.Value);
            vm.TargetNumberOfRaces = await _mergeService.GetNumberOfRaces(vm.TargetCompetitorId.Value);

            vm.SourceSeasons = await _mergeService.GetSeasons(vm.SourceCompetitorId.Value);
            vm.TargetSeasons = await _mergeService.GetSeasons(vm.TargetCompetitorId.Value);

            return View("Verify", vm);
        }

        [HttpPost]
        public async Task<ActionResult> Merge(
            string clubInitials,
            MergeCompetitorViewModel vm)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id))
            {
                return Unauthorized();
            }
            vm.SourceCompetitor = await _competitorService.GetCompetitorAsync(vm.SourceCompetitorId.Value);
            vm.TargetCompetitor = await _competitorService.GetCompetitorAsync(vm.TargetCompetitorId.Value);
            if (vm.SourceCompetitor.ClubId != club.Id ||
                vm.TargetCompetitor.ClubId != club.Id)
            {
                return Unauthorized();
            }
            await _mergeService.Merge(vm.TargetCompetitorId, vm.SourceCompetitorId);
            vm.TargetNumberOfRaces = await _mergeService.GetNumberOfRaces(vm.TargetCompetitorId);

            return View("Done", vm);
        }
    }
}
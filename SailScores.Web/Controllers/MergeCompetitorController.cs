using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SailScores.Web.Models.SailScores;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Controllers
{
    [Authorize]
    public class MergeCompetitorController : Controller
    {
        private readonly Core.Services.IClubService _clubService;
        private readonly Web.Services.ICompetitorService _competitorService;
        private readonly Services.IAuthorizationService _authService;
        private readonly Services.IMergeService _mergeService;

        public MergeCompetitorController(
            Core.Services.IClubService clubService,
            Web.Services.ICompetitorService competitorService,
            Services.IAuthorizationService authService,
            Services.IMergeService mergeService)
        {
            _clubService = clubService;
            _competitorService = competitorService;
            _authService = authService;
            _mergeService = mergeService;
        }

        public async Task<ActionResult> Options(string clubInitials)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }
            IList<Core.Model.Competitor> competitors = await _competitorService.GetCompetitorsAsync(clubId);
            var vm = new MergeCompetitorViewModel
            {
                TargetCompetitorOptions = competitors.OrderBy(c => c.Name).ToList()
            };
            return View("SelectTarget", vm);
        }

        [HttpPost]
        public async Task<ActionResult> Options(
            string clubInitials, 
            MergeCompetitorViewModel vm)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
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
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }

            vm.SourceCompetitor = await _competitorService.GetCompetitorAsync(vm.SourceCompetitorId.Value);
            vm.TargetCompetitor = await _competitorService.GetCompetitorAsync(vm.TargetCompetitorId.Value);
            if(vm.SourceCompetitor.ClubId != clubId ||
                vm.TargetCompetitor.ClubId != clubId)
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
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }
            vm.SourceCompetitor = await _competitorService.GetCompetitorAsync(vm.SourceCompetitorId.Value);
            vm.TargetCompetitor = await _competitorService.GetCompetitorAsync(vm.TargetCompetitorId.Value);
            if (vm.SourceCompetitor.ClubId != clubId ||
                vm.TargetCompetitor.ClubId != clubId)
            {
                return Unauthorized();
            }
            await _mergeService.Merge(vm.TargetCompetitorId, vm.SourceCompetitorId);
            vm.TargetNumberOfRaces = await _mergeService.GetNumberOfRaces(vm.TargetCompetitorId);

            return View("Done", vm);
        }
    }
}
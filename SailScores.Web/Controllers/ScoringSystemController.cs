using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Controllers
{
    [Authorize]
    public class ScoringSystemController : Controller
    {

        private readonly IClubService _clubService;
        private readonly IScoringService _scoringService;
        private readonly IMapper _mapper;
        private readonly Services.IAuthorizationService _authService;

        public ScoringSystemController(
            IClubService clubService,
            IScoringService scoringService,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _clubService = clubService;
            _scoringService = scoringService;
            _authService = authService;
            _mapper = mapper;
        }

        public async Task<ActionResult> Create(
            string clubInitials)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }
            var vm = new ScoringSystemWithOptionsViewModel
            {
                ClubId = clubId,
                DiscardPattern = "0"
            };
            var potentialParents = await _scoringService.GetScoringSystemsAsync(clubId, true);
            vm.ParentSystemOptions = potentialParents.ToList();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(
            string clubInitials,
            ScoringSystemWithOptionsViewModel model)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }
            model.ClubId = clubId;
            model.Id = Guid.NewGuid();
            if (!ModelState.IsValid)
            {
                var potentialParents = await _scoringService.GetScoringSystemsAsync(clubId, true);
                model.ParentSystemOptions = potentialParents.ToList();
                return View(model);
            }

            await _scoringService.SaveScoringSystemAsync(
            _mapper.Map<ScoringSystem>(model));

            return RedirectToAction("Edit", "ScoringSystem", new { id = model.Id });
        }

        public async Task<ActionResult> Edit(string clubInitials, Guid id)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }

            var scoringSystem = await _scoringService.GetScoringSystemAsync(id);
            if (scoringSystem.ClubId != clubId)
            {
                return Unauthorized();
            }

            var vm = _mapper.Map<ScoringSystemWithOptionsViewModel>(scoringSystem);
            var potentialParents = await _scoringService.GetScoringSystemsAsync(clubId, true);
            vm.ParentSystemOptions = potentialParents.Where(s => s.Id != id).ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(
            string clubInitials,
            ScoringSystemWithOptionsViewModel model)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId)
                || model.ClubId != clubId)
            {
                return Unauthorized();
            }
            if (!ModelState.IsValid)
            {
                var potentialParents = await _scoringService.GetScoringSystemsAsync(clubId, true);
                model.ParentSystemOptions = potentialParents.ToList();

                var scoringSystem = await _scoringService.GetScoringSystemAsync(model.Id);
                model.ScoreCodes = scoringSystem.ScoreCodes;
                model.InheritedScoreCodes = scoringSystem.InheritedScoreCodes;
                return View(model);
            }
            var system = _mapper.Map<ScoringSystem>(model);
            await _scoringService.SaveScoringSystemAsync(system);

            return RedirectToAction("Index", "Admin");

        }

        public async Task<ActionResult> Delete(string clubInitials, Guid id)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }

            var scoringSystem = await _scoringService.GetScoringSystemAsync(id);
            if (scoringSystem.ClubId != clubId)
            {
                return Unauthorized();
            }

            var vm = _mapper.Map<ScoringSystemCanBeDeletedViewModel>(scoringSystem);
            vm.InUse = await _scoringService.IsScoringSystemInUseAsync(vm.Id);

            return View(vm);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }

            var scoringSystem = await _scoringService.GetScoringSystemAsync(id);
            if (scoringSystem.ClubId != clubId)
            {
                return Unauthorized();
            }

            if (await _scoringService.IsScoringSystemInUseAsync(id))
            {
                return StatusCode(500);
            }

            await _scoringService.DeleteScoringSystemAsync(id);
            return RedirectToAction("Index", "Admin");
        }
    }
}
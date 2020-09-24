using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Controllers
{
    public class RegattaController : Controller
    {

        private readonly Web.Services.IRegattaService _regattaService;
        private readonly Core.Services.IClubService _clubService;
        private readonly Services.IAuthorizationService _authService;
        private readonly IMapper _mapper;

        public RegattaController(
            Web.Services.IRegattaService regattaService,
            Core.Services.IClubService clubService,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _regattaService = regattaService;
            _clubService = clubService;
            _authService = authService;
            _mapper = mapper;
        }

        [ResponseCache(Duration = 3600)]
        public async Task<ActionResult> Index(string clubInitials)
        {
            ViewData["ClubInitials"] = clubInitials;

            var regattas = await _regattaService.GetAllRegattaSummaryAsync(clubInitials);

            return View(new ClubCollectionViewModel<RegattaSummaryViewModel>
            {
                List = regattas,
                ClubInitials = clubInitials
            });
        }

        public async Task<ActionResult> Details(
            string clubInitials,
            string season,
            string regattaName)
        {
            ViewData["ClubInitials"] = clubInitials;

            var regatta = await _regattaService.GetRegattaAsync(clubInitials, season, regattaName);
            if (regatta == null)
            {
                return new NotFoundResult();
            }

            var canEdit = false;
            if (User != null && (User.Identity?.IsAuthenticated ?? false))
            {
                canEdit = await _authService.CanUserEdit(User, clubInitials);
            }

            return View(new ClubItemViewModel<RegattaViewModel>
            {
                Item = _mapper.Map<RegattaViewModel>(regatta),
                ClubInitials = clubInitials,
                CanEdit = canEdit
            });
        }

        [Authorize]
        public async Task<ActionResult> Create(string clubInitials)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }

            var vm = await _regattaService.GetBlankRegattaWithOptions(clubId);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> Create(
            string clubInitials,
            RegattaWithOptionsViewModel model)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            try
            {
                if (!await _authService.CanUserEdit(User, clubId))
                {
                    return Unauthorized();
                }
                model.ClubId = clubId;
                if (!ModelState.IsValid)
                {
                    var blank = await _regattaService.GetBlankRegattaWithOptions(clubId);

                    model.SeasonOptions = blank.SeasonOptions;
                    model.ScoringSystemOptions = blank.ScoringSystemOptions;
                    model.FleetOptions = blank.FleetOptions;

                    return View(model);
                }

                var regattaId = await _regattaService.SaveNewAsync(model);
                var savedRegatta = await _regattaService.GetRegattaAsync(regattaId);
                return RedirectToAction("Details", new
                {
                    clubInitials,
                    season = savedRegatta.Season.UrlName,
                    regattaName = savedRegatta.UrlName
                });
            }
            catch
            {

                var blank = await _regattaService.GetBlankRegattaWithOptions(clubId);

                model.SeasonOptions = blank.SeasonOptions;
                model.ScoringSystemOptions = blank.ScoringSystemOptions;
                model.FleetOptions = blank.FleetOptions;

                return View(model);
            }
        }

        [Authorize]
        public async Task<ActionResult> Edit(
            string clubInitials,
            Guid id)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }
            var regatta = await _regattaService.GetRegattaAsync(id);
            if (regatta == null)
            {
                return NotFound();
            }

            var blankVm = await _regattaService.GetBlankRegattaWithOptions(clubId);
            var vm = _mapper.Map<RegattaWithOptionsViewModel>(regatta);
            vm.SeasonOptions = blankVm.SeasonOptions;
            vm.ScoringSystemOptions = blankVm.ScoringSystemOptions;
            vm.FleetOptions = blankVm.FleetOptions;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> Edit(
            string clubInitials,
            RegattaWithOptionsViewModel model)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            try
            {
                var regatta = await _regattaService.GetRegattaAsync(model.Id);
                if (!await _authService.CanUserEdit(User, clubId)
                    || regatta.ClubId != clubId)
                {
                    return Unauthorized();
                }

                if (!ModelState.IsValid)
                {
                    var blankVm = await _regattaService.GetBlankRegattaWithOptions(clubId);
                    model.SeasonOptions = blankVm.SeasonOptions;
                    model.ScoringSystemOptions = blankVm.ScoringSystemOptions;
                    model.FleetOptions = blankVm.FleetOptions;
                    return View(model);
                }

                var regattaId = await _regattaService.UpdateAsync(model);
                var savedRegatta = await _regattaService.GetRegattaAsync(regattaId);
                return RedirectToAction("Details", new
                {
                    clubInitials,
                    season = savedRegatta.Season.UrlName,
                    regattaName = savedRegatta.UrlName
                });
            }
            catch
            {
                return View(model);
            }
        }

        [Authorize]
        public async Task<ActionResult> Delete(string clubInitials, Guid id)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            var regatta = await _regattaService.GetRegattaAsync(id);
            if (!await _authService.CanUserEdit(User, clubId)
                || regatta?.ClubId != clubId)
            {
                return Unauthorized();
            }

            return View(regatta);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            var regatta = await _regattaService.GetRegattaAsync(id);
            if (!await _authService.CanUserEdit(User, clubId)
                || regatta?.ClubId != clubId)
            {
                return Unauthorized();
            }
            try
            {
                await _regattaService.DeleteAsync(id);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                ModelState.AddModelError("Exception", "A problem occured while deleting.");
                return View(regatta);
            }
        }
    }
}
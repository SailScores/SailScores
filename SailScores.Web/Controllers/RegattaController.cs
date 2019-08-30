using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;

namespace SailScores.Web.Controllers
{
    public class RegattaController : Controller
    {

        private readonly Web.Services.IRegattaService _regattaService;
        private readonly Core.Services.IClubService _clubService;
        private readonly Services.IAuthorizationService _authService;
        private readonly IScoringService _scoringService;
        private readonly IMapper _mapper;

        public RegattaController(
            Web.Services.IRegattaService regattaService,
            Core.Services.IClubService clubService,
            Services.IAuthorizationService authService,
            IScoringService scoringService,
            IMapper mapper)
        {
            _regattaService = regattaService;
            _clubService = clubService;
            _authService = authService;
            _scoringService = scoringService;
            _mapper = mapper;
        }

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

            var canEdit = false;
            if (User != null && (User.Identity?.IsAuthenticated ?? false))
            {
                var clubId = await _clubService.GetClubId(clubInitials);
                canEdit = await _authService.CanUserEdit(User, clubId);
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
            var club = await _clubService.GetFullClub(clubInitials);
            var vm = new RegattaWithOptionsViewModel
            {
                SeasonOptions = club.Seasons
            };
            var scoringSystemOptions = await _scoringService.GetScoringSystemsAsync(club.Id, true);
            scoringSystemOptions.Add(new ScoringSystem
            {
                Id = Guid.Empty,
                Name = "<Use Club Default>"
            });
            vm.ScoringSystemOptions = scoringSystemOptions.OrderBy(s => s.Name).ToList();
            vm.FleetOptions = club.Fleets;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> Create(string clubInitials, RegattaWithOptionsViewModel model)
        {
            try
            {
                var clubId = await _clubService.GetClubId(clubInitials);
                if (!await _authService.CanUserEdit(User, clubId))
                {
                    return Unauthorized();
                }
                model.ClubId = clubId;
                await _regattaService.SaveNewAsync(model);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                var club = await _clubService.GetFullClub(clubInitials);
                model.SeasonOptions = club.Seasons;
                var scoringSystemOptions = await _scoringService.GetScoringSystemsAsync(club.Id, true);
                scoringSystemOptions.Add(new ScoringSystem
                {
                    Id = Guid.Empty,
                    Name = "<Use Club Default>"
                });
                model.ScoringSystemOptions = scoringSystemOptions.OrderBy(s => s.Name).ToList();
                return View(model);
            }
        }

        [Authorize]
        public async Task<ActionResult> Edit(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id))
            {
                return Unauthorized();
            }
            var regatta =
                club.Regattas
                .SingleOrDefault(c => c.Id == id);
            if (regatta == null)
            {
                return NotFound();
            }

            var vm = _mapper.Map<RegattaWithOptionsViewModel>(regatta);
            vm.SeasonOptions = club.Seasons;
            var scoringSystemOptions = await _scoringService.GetScoringSystemsAsync(club.Id, true);
            scoringSystemOptions.Add(new ScoringSystem
            {
                Id = Guid.Empty,
                Name = "<Use Club Default>"
            });
            vm.ScoringSystemOptions = scoringSystemOptions.OrderBy(s => s.Name).ToList();
            vm.FleetOptions = club.Fleets;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> Edit(string clubInitials, RegattaWithOptionsViewModel model)
        {
            try
            {
                var club = await _clubService.GetFullClub(clubInitials);
                if (!await _authService.CanUserEdit(User, club.Id)
                    || !club.Regattas.Any(r => r.Id == model.Id))
                {
                    return Unauthorized();
                }
                await _regattaService.UpdateAsync(model);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                return View(model);
            }
        }

        [Authorize]
        public async Task<ActionResult> Delete(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id)
                || !club.Regattas.Any(c => c.Id == id))
            {
                return Unauthorized();
            }
            var regatta = club.Regattas.SingleOrDefault(c => c.Id == id);
            if (regatta == null)
            {
                return NotFound();
            }
            return View(regatta);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id)
                || !club.Regattas.Any(c => c.Id == id))
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
                return View();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using CoreServices = SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;

namespace SailScores.Web.Controllers
{
    public class FleetController : Controller
    {


        private readonly CoreServices.IClubService _clubService;
        private readonly IFleetService _fleetService;
        private readonly IMapper _mapper;
        private readonly IAuthorizationService _authService;

        public FleetController(
                CoreServices.IClubService clubService,
            IFleetService fleetService,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _clubService = clubService;
            _fleetService = fleetService;
            _authService = authService;
            _mapper = mapper;
        }

        public async Task<ActionResult> Index(string clubInitials)
        {
            ViewData["ClubInitials"] = clubInitials;

            var fleet = await _fleetService.GetAllFleetSummary(clubInitials);

            return View(new ClubCollectionViewModel<FleetSummary>
            {
                List = fleet,
                ClubInitials = clubInitials
            });
        }

        public async Task<ActionResult> Details(
            string clubInitials,
            string fleetShortName)
        {
            ViewData["ClubInitials"] = clubInitials;

            var fleet = await _fleetService.GetFleet(clubInitials, fleetShortName);

            return View(new ClubItemViewModel<FleetSummary>
            {
                Item = fleet,
                ClubInitials = clubInitials
            });
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(string clubInitials, Fleet model)
        {
            try
            {
                var club = (await _clubService.GetClubs(true)).Single(c => c.Initials == clubInitials);
                if (!await _authService.CanUserEdit(User, club.Id))
                {
                    return Unauthorized();
                }
                model.ClubId = club.Id;
                await _fleetService.SaveNew(model);

                return RedirectToAction(nameof(Edit), "Admin");
            }
            catch
            {
                return View();
            }
        }

        public async Task<ActionResult> Edit(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id))
            {
                return Unauthorized();
            }
            var fleet =
                club.Fleets
                .Single(c => c.Id == id);
            return View(fleet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string clubInitials, Fleet model)
        {
            try
            {
                var club = await _clubService.GetFullClub(clubInitials);
                if (!await _authService.CanUserEdit(User, club.Id)
                    || !club.Fleets.Any(c => c.Id == model.Id))
                {
                    return Unauthorized();
                }
                await _fleetService.Update(model);

                return RedirectToAction(nameof(Edit), "Admin");
            }
            catch
            {
                return View(model);
            }
        }

        public async Task<ActionResult> Delete(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id)
                || !club.Fleets.Any(c => c.Id == id))
            {
                return Unauthorized();
            }
            var boatClass = club.Fleets.Single(c => c.Id == id);
            return View(boatClass);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id)
                || !club.Fleets.Any(c => c.Id == id))
            {
                return Unauthorized();
            }
            try
            {
                await _fleetService.Delete(id);

                return RedirectToAction(nameof(Edit), "Admin");
            }
            catch
            {
                return View();
            }
        }
    }
}
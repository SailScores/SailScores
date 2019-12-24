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

namespace SailScores.Web.Controllers
{
    [Authorize]
    public class BoatClassController : Controller
    {

        private readonly IClubService _clubService;
        private readonly IBoatClassService _classService;
        private readonly IMapper _mapper;
        private readonly Services.IAuthorizationService _authService;

        public BoatClassController(
            IClubService clubService,
            IBoatClassService classService,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _clubService = clubService;
            _classService = classService;
            _authService = authService;
            _mapper = mapper;
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(string clubInitials, BoatClass model)
        {
            try
            {
                var club = (await _clubService.GetClubs(true)).Single(c =>
                    c.Initials == clubInitials);
                if (!await _authService.CanUserEdit(User, club.Id))
                {
                    return Unauthorized();
                }
                model.ClubId = club.Id;
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                await _classService.SaveNew(model);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                return View();
            }
        }

        public async Task< ActionResult> Edit(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id))
            {
                return Unauthorized();
            }
            var boatClass =
                club.BoatClasses
                .Single(c => c.Id == id);
            return View(boatClass);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string clubInitials, BoatClass model)
        {
            try
            {
                var club = await _clubService.GetFullClub(clubInitials);
                if (!await _authService.CanUserEdit(User, club.Id)
                    || !club.BoatClasses.Any(c => c.Id == model.Id))
                {
                    return Unauthorized();
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                await _classService.Update(model);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                return View();
            }
        }

        public async Task<ActionResult> Delete(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id)
                || !club.BoatClasses.Any(c => c.Id == id))
            {
                return Unauthorized();
            }
            var boatClass = club.BoatClasses.Single(c => c.Id == id);
            //todo: add blocker if class contains boats. (or way to move boats.)
            return View(boatClass);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id)
                || !club.BoatClasses.Any(c => c.Id == id))
            {
                return Unauthorized();
            }
            try
            {
                await _classService.Delete(id);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                return View();
            }
        }
    }
}
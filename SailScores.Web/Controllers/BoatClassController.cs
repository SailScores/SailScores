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
        private readonly IMapper _mapper;
        private readonly Services.IAuthorizationService _authService;

        public BoatClassController(
            IClubService clubService,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _clubService = clubService;
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
                var club = (await _clubService.GetClubs(true)).Single(c => c.Initials == clubInitials);
                if (!await _authService.CanUserEdit(User, club.Id))
                {
                    return Unauthorized();
                }
                model.ClubId = club.Id;
                await _clubService.SaveNewBoatClass(model);

                return RedirectToAction(nameof(Edit), "Admin");
            }
            catch
            {
                return View();
            }
        }

        public ActionResult Edit(Guid id)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string clubInitials, BoatClass model)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Edit), "Admin");
            }
            catch
            {
                return View();
            }
        }

        public async Task<ActionResult> Delete(string clubInitials, Guid id)
        {
            var club = (await _clubService.GetClubs(true)).Single(c => c.Initials == clubInitials);
            var boatClass = club.BoatClasses.Single(c => c.Id == id);
            //todo: add blocker if class contains boats. (or way to move boats.)
            return View(boatClass);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string clubInitials, BoatClass model)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Edit), "Admin");
            }
            catch
            {
                return View();
            }
        }
    }
}
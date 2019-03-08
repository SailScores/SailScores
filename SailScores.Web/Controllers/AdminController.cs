using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Dtos;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using Services = SailScores.Web.Services;

namespace SailScores.Web.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {

        private readonly IClubService _clubService;
        private readonly Services.IAuthorizationService _authService;
        private readonly IMapper _mapper;

        public AdminController(
            IClubService clubService,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _clubService = clubService;
            _authService = authService;
            _mapper = mapper;
        }

        // GET: Admin
        public async Task<ActionResult> Index(string clubInitials)
        {
            ViewData["ClubInitials"] = clubInitials;

            var club = await _clubService.GetFullClub(clubInitials);
            return View(_mapper.Map<AdminViewModel>(club));
        }

       
        // GET: Admin/Edit/5
        public async Task<ActionResult> Edit(string clubInitials)
        {
            ViewData["ClubInitials"] = clubInitials;

            var club = await _clubService.GetFullClub(clubInitials);
            return View(_mapper.Map<AdminViewModel>(club));
        }

        // POST: Admin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string clubInitials, AdminViewModel clubAdmin)
        {
            ViewData["ClubInitials"] = clubInitials;
            try
            {
                if (!await _authService.CanUserEdit(User, clubAdmin.Id))
                {
                    return Unauthorized();
                }
                var clubObject = _mapper.Map<Club>(clubAdmin);
                await _clubService.UpdateClub(clubObject);

                return RedirectToAction(nameof(Index), "Admin", new { clubInitials = clubInitials });
            }
            catch
            {
                return View();
            }
        }

    }
}
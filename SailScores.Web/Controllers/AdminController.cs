using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {

        private readonly IClubService _clubService;
        private readonly IMapper _mapper;

        public AdminController(
            IClubService clubService,
            IMapper mapper)
        {
            _clubService = clubService;
            _mapper = mapper;
        }

        // GET: Admin
        public ActionResult Index()
        {
            return View();
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
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

    }
}
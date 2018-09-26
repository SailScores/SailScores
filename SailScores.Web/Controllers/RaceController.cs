using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;

namespace SailScores.Web.Controllers
{
    public class RaceController : Controller
    {

        private readonly IRaceService _raceService;

        public RaceController(
            IRaceService raceService)
        {
            _raceService = raceService;
        }

        // GET: Club
        public async Task<ActionResult> Index(string clubInitials)
        {
            ViewData["ClubInitials"] = clubInitials;

            //TODO: Implement
            throw new NotImplementedException();
            //var races = await _raceService.GetAllRaceSummariesAsync(clubInitials);
            //return View(races);
        }

        // GET: Club/Details/{5126225B-77AC-40EC-8FDA-9549AF7AE738}
        public ActionResult Details(Guid id)
        {
            return View();
        }


    }
}
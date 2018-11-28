using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sailscores.Core.Services;
using Sailscores.Web.Models.Sailscores;
using Sailscores.Web.Services;

namespace Sailscores.Web.Controllers
{
    public class RaceController : Controller
    {

        private readonly Web.Services.IRaceService _raceService;

        public RaceController(
            Web.Services.IRaceService raceService)
        {
            _raceService = raceService;
        }

        // GET: Club
        public async Task<ActionResult> Index(string clubInitials)
        {
            var races = await _raceService.GetAllRaceSummariesAsync(clubInitials);

            return View(new ClubCollectionViewModel<Core.Model.Race>
            {
                List = races,
                ClubInitials = clubInitials
            });
        }

        // GET: Club/Details/{5126225B-77AC-40EC-8FDA-9549AF7AE738}
        public async Task<ActionResult> Details(string clubInitials, Guid id)
        {
            var race = await _raceService.GetSingleRaceDetailsAsync(clubInitials, id);
            return View(new ClubItemViewModel<RaceViewModel>
            {
                Item = race,
                ClubInitials = clubInitials
            });
        }


    }
}
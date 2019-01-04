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
    public class FleetController : Controller
    {

        private readonly Web.Services.IFleetService _fleetService;

        public FleetController(
            Web.Services.IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        // GET: Series
        public async Task<ActionResult> Index(string clubInitials)
        {
            ViewData["ClubInitials"] = clubInitials;

            var fleet = await _fleetService.GetAllFleetSummaryAsync(clubInitials);

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

            var fleet = await _fleetService.GetFleetAsync(clubInitials, fleetShortName);

            return View(new ClubItemViewModel<FleetSummary>
            {
                Item = fleet,
                ClubInitials = clubInitials
            });
        }
    }
}
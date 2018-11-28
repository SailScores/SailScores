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
    public class SeriesController : Controller
    {

        private readonly Web.Services.ISeriesService _seriesService;

        public SeriesController(
            Web.Services.ISeriesService seriesService)
        {
            _seriesService = seriesService;
        }

        // GET: Series
        public async Task<ActionResult> Index(string clubInitials)
        {
            ViewData["ClubInitials"] = clubInitials;

            var series = await _seriesService.GetAllSeriesSummaryAsync(clubInitials);

            return View(new ClubCollectionViewModel<SeriesSummary>
            {
                List = series,
                ClubInitials = clubInitials
            });
        }

        public async Task<ActionResult> Details(
            string clubInitials,
            string season,
            string seriesName)
        {
            ViewData["ClubInitials"] = clubInitials;

            var series = await _seriesService.GetSeriesAsync(clubInitials, season, seriesName);

            return View(new ClubItemViewModel<Core.Model.Series>
            {
                Item = series,
                ClubInitials = clubInitials
            });
        }
    }
}
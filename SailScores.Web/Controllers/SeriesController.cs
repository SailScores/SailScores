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
    public class SeriesController : Controller
    {

        private readonly ISeriesService _seriesService;

        public SeriesController(
            ISeriesService seriesService)
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

            return View(new ClubItemViewModel<SeriesSummary>
            {
                Item = series,
                ClubInitials = clubInitials
            });
        }
    }
}
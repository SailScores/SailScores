using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Controllers
{
    public class ClubController : Controller
    {
        private readonly IClubService _clubService;
        private readonly IMapper _mapper;

        public ClubController(
            IClubService clubService,
            IMapper mapper)
        {
            _clubService = clubService;
            _mapper = mapper;
        }
        
        // GET: Club
        public async Task<ActionResult> Index(string clubInitials)
        {
            ViewData["ClubInitials"] = clubInitials;

            var club = await _clubService.GetFullClub(clubInitials);
            return View(_mapper.Map<ClubSummaryViewModel>(club));
        }

    }
}
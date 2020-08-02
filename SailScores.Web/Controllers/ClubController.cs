using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;

namespace SailScores.Web.Controllers
{
    public class ClubController : Controller
    {
        private readonly IClubService _clubService;
        private readonly Services.IAuthorizationService _authService;
        private readonly IMapper _mapper;

        public ClubController(
            IClubService clubService,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _clubService = clubService;
            _authService = authService;
            _mapper = mapper;
        }

        // GET: Club
        public async Task<ActionResult> Index(string clubInitials)
        {
            ViewData["ClubInitials"] = clubInitials;

            var club = await _clubService.GetClubForClubHome(clubInitials);
            var viewModel = _mapper.Map<ClubSummaryViewModel>(club);
            viewModel.CanEdit = await _authService.CanUserEdit(User, clubInitials);
            return View(viewModel);
        }

        // GET: Club
        public async Task<ActionResult> Stats(string clubInitials)
        {
            ViewData["ClubInitials"] = clubInitials;
            //TODO
            return View(viewModel);
        }
    }
}
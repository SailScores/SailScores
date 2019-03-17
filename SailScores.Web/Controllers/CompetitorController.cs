using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Controllers
{
    [Authorize]
    public class CompetitorController : Controller
    {
        private readonly IClubService _clubService;
        private readonly ICompetitorService _competitorService;
        private readonly IMapper _mapper;
        private readonly Services.IAuthorizationService _authService;

        public CompetitorController(
            IClubService clubService,
            ICompetitorService competitorService,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _clubService = clubService;
            _competitorService = competitorService;
            _authService = authService;
            _mapper = mapper;
        }

        // GET: Competitor
        public ActionResult Index(string clubInitials)
        {
            return View();
        }

        // GET: Competitor/Details/5
        public ActionResult Details(string clubInitials, Guid id)
        {
            return View();
        }

        // GET: Competitor/Create
        public async Task<ActionResult> Create(string clubInitials)
        {
            var comp = new CompetitorWithOptionsViewModel();
            var club = await _clubService.GetFullClub(clubInitials);
            comp.BoatClassOptions = club.BoatClasses.OrderBy(c => c.Name);

            return View(comp);
        }

        // POST: Competitor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(string clubInitials, Competitor competitor)
        {
            try
            {
                var club = (await _clubService.GetClubs(true)).Single(c => 
                    c.Initials.ToUpperInvariant() == clubInitials.ToUpperInvariant());
                if (!await _authService.CanUserEdit(User, club.Id))
                {
                    return Unauthorized();
                }
                competitor.ClubId = club.Id;
                await _competitorService.SaveAsync(competitor);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                return View();
            }
        }

        // GET: Competitor/Edit/5
        public async Task<ActionResult> Edit(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id))
            {
                return Unauthorized();
            }
            var competitor = await _competitorService.GetCompetitorAsync(id);
            if(competitor.ClubId != club.Id)
            {
                return Unauthorized();
            }
            var compWithOptions = _mapper.Map<CompetitorWithOptionsViewModel>(competitor);

            compWithOptions.BoatClassOptions = club.BoatClasses.OrderBy(c => c.Name);
            return View(compWithOptions);
        }

        // POST: Competitor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, Competitor competitor)
        {
            try
            {
                if (!await _authService.CanUserEdit(User, competitor.ClubId))
                {
                    return Unauthorized();
                }
                await _competitorService.SaveAsync(competitor);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                return View();
            }
        }

        [HttpGet]
        // GET: Competitor/Delete/5
        public async Task<ActionResult> Delete(string clubInitials, Guid id)
        {
            var club = (await _clubService.GetClubs(true)).Single(c =>
                c.Initials.ToUpperInvariant() == clubInitials.ToUpperInvariant());
            if (!await _authService.CanUserEdit(User, club.Id))
            {
                return Unauthorized();
            }
            var competitor = await _competitorService.GetCompetitorAsync(id);
            if (competitor.ClubId != club.Id)
            {
                return Unauthorized();
            }
            return View(competitor);
        }

        // POST: Competitor/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
        {
            try
            {
                var club = await _clubService.GetFullClub(clubInitials);
                if (!await _authService.CanUserEdit(User, club.Id)
                    || !club.Competitors.Any(c => c.Id == id))
                {
                    return Unauthorized();
                }
                await _competitorService.DeleteCompetitorAsync(id);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                return View();
            }
        }
    }
}
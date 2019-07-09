using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Controllers
{
    [Authorize]
    public class ScoreCodeController : Controller
    {

        private readonly IClubService _clubService;
        private readonly IScoringService _scoringService;
        private readonly IMapper _mapper;
        private readonly Services.IAuthorizationService _authService;

        public ScoreCodeController(
            IClubService clubService,
            IScoringService scoringService,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _clubService = clubService;
            _scoringService = scoringService;
            _authService = authService;
            _mapper = mapper;
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(string clubInitials, Season model)
        {
            throw new NotImplementedException();
        }

        public async Task<ActionResult> Edit(
            string clubInitials,
            Guid id,
            string returnUrl = null)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }

            var scoreCode = await _scoringService.GetScoreCodeAsync(id);

            if(scoreCode == null)
            {
                return new NotFoundResult();
            }
            ViewData["ReturnUrl"] = returnUrl;

            var vm = _mapper.Map<ScoreCodeWithOptionsViewModel>(scoreCode);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string clubInitials)
        {
            throw new NotImplementedException();
        }

        public async Task<ActionResult> Delete(string clubInitials, Guid id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
        {
            throw new NotImplementedException();

        }
    }
}
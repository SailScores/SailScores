using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;

namespace SailScores.Web.Controllers
{
    public class ClubRequestController : Controller
    {
        private readonly IClubRequestService _clubRequestService;
        private readonly Services.IAuthorizationService _authService;

        public ClubRequestController(
            IClubRequestService clubRequestService,
            Services.IAuthorizationService authService)
        {
            _clubRequestService = clubRequestService;
            _authService = authService;
        }
        
        public async Task<ActionResult> Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Index(ClubRequestViewModel request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            await _clubRequestService.SubmitRequest(request);

            return View("RequestSubmitted", request);
        }


        public async Task<ActionResult> List()
        {
            if (!await _authService.IsUserFullAdmin(User))
            {
                return Unauthorized();
            }

            IList<ClubRequestViewModel> vm = await _clubRequestService.GetPendingRequests();

            return View(vm);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            if (!await _authService.IsUserFullAdmin(User))
            {
                return Unauthorized();
            }

            var vm = await _clubRequestService.GetRequest(id);

            return View(vm);
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            if (!await _authService.IsUserFullAdmin(User))
            {
                return Unauthorized();
            }

            var vm = await _clubRequestService.GetRequest(id);

            return View(vm);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(ClubRequestWithOptionsViewModel vm)
        {
            if (!await _authService.IsUserFullAdmin(User))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                var vmWithOptions = await _clubRequestService.GetRequest(vm.Id);
                vm.ClubOptions = vmWithOptions.ClubOptions;
                return View(vm);
            }
            await _clubRequestService.UpdateRequest(vm);

            return await Details(vm.Id);
        }

        [HttpPost]
        public async Task<ActionResult> CreateClub(Guid id,
            bool test,
            Guid copyFromClubId)
        {
            if (!await _authService.IsUserFullAdmin(User))
            {
                return Unauthorized();
            }

            await _clubRequestService.ProcessRequest(id, test,
                copyFromClubId);

            var vm = await _clubRequestService.GetRequest(id);
            vm.ForTesting = test;

            return View(vm);
        }
        
    }
}
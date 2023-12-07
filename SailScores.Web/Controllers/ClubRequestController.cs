using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SailScores.Identity.Entities;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Controllers;

public class ClubRequestController : Controller
{
    private readonly IClubRequestService _clubRequestService;
    private readonly IAuthorizationService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public ClubRequestController(
        IClubRequestService clubRequestService,
        IAuthorizationService authService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger)
    {
        _clubRequestService = clubRequestService;
        _authService = authService;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;

    }

    public async Task<ActionResult> Index()
    {
        if (User?.Identity?.IsAuthenticated ?? false)
        {
            var email = User?.FindFirst("sub")?.Value ?? User.Identity.Name;

            return View("RequestClub", new ClubRequestViewModel
            {
                ContactEmail = email
            });
        }
        return View("RequestAccountAndClub");
    }

    [HttpPost]
    public async Task<ActionResult> RequestClub(ClubRequestViewModel request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        if (!(await _clubRequestService.AreInitialsAllowed(request.ClubInitials)))
        {
            ModelState.AddModelError("ClubInitials", "Initials not valid.");
            return View(request);
        }

        await _clubRequestService.SubmitRequest(request);

        return View("RequestSubmitted", request);
    }

    [HttpPost]
    public async Task<ActionResult> RequestAccountAndClub(AccountAndClubRequestViewModel request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        if (!(await _clubRequestService.AreInitialsAllowed(request.ClubInitials)))
        {
            ModelState.AddModelError("ClubInitials", "Initials are not valid.");
            return View(request);
        }

        var user = new ApplicationUser
        {
            UserName = request.ContactEmail,
            Email = request.ContactEmail,
            FirstName = request.ContactFirstName,
            LastName = request.ContactLastName,
            EnableAppInsights = request.EnableAppInsights
        };
        var result = await _userManager.CreateAsync(user, request.Password);
        if(!result.Succeeded)
        {
            foreach (var e in result.Errors)
            {
                if (e.Description.Contains("Username") &&
                    e.Description.Contains("already taken"))
                {
                    ModelState.AddModelError(String.Empty, "This account already exists. Please " +
                                                 "sign in and then start club creation.");
                }
                else if (e.Description.Contains("Username"))
                {
                    ModelState.AddModelError("ContactEmail", e.Description);
                } else
                {
                    ModelState.AddModelError(String.Empty, e.Description);
                }
            }
            return View(request);

        }
        if (result.Succeeded)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User created a new account with password during club request.");
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

    [AcceptVerbs("GET", "POST")]
    public async Task<IActionResult> VerifyInitials(string clubInitials)
    {
        if (!await _clubRequestService.AreInitialsAllowed(clubInitials))
        {
            return Json($"Not available. The initials must be alphanumeric and not in use.");
        }

        return Json(true);
    }
}
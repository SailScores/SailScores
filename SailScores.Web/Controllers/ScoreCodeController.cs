using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class ScoreCodeController : Controller
{

    private readonly IClubService _clubService;
    private readonly IScoringService _scoringService;
    private readonly IMapper _mapper;
    private readonly IAuthorizationService _authService;

    public ScoreCodeController(
        IClubService clubService,
        IScoringService scoringService,
        IAuthorizationService authService,
        IMapper mapper)
    {
        _clubService = clubService;
        _scoringService = scoringService;
        _authService = authService;
        _mapper = mapper;
    }

    public async Task<ActionResult> Override(
        string clubInitials,
        Guid scoringSystemId,
        string code,
        string returnUrl = null)
    {

        var clubId = await _clubService.GetClubId(clubInitials);
        ViewData["ReturnUrl"] = returnUrl;


        var scoringSystem = await _scoringService.GetScoringSystemAsync(scoringSystemId);
        if (scoringSystem.ClubId != clubId)
        {
            return Unauthorized();
        }
        var vm = _mapper.Map<ScoreCodeWithOptionsViewModel>(scoringSystem.InheritedScoreCodes
            .FirstOrDefault(sc => sc.Name == code));
        vm.ClubId = clubId;
        vm.ScoringSystemId = scoringSystemId;
        vm.Name = code;
        vm.Id = Guid.Empty;
        return View(vm);
    }

    public async Task<ActionResult> Create(
        string clubInitials,
        Guid scoringSystemId,
        string returnUrl = null)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        ViewData["ReturnUrl"] = returnUrl;

        var scoringSystem = await _scoringService.GetScoringSystemAsync(scoringSystemId);
        if (scoringSystem.ClubId != clubId)
        {
            return Unauthorized();
        }
        var vm = new ScoreCodeWithOptionsViewModel
        {
            ClubId = clubId,
            ScoringSystemId = scoringSystemId
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(
        string clubInitials,
        ScoreCodeWithOptionsViewModel model,
        string returnUrl)
    {

        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.IsUserClubAdministrator(User, clubId))
        {
            return Unauthorized();
        }
        var scoreSystem = await _scoringService.GetScoringSystemAsync(model.ScoringSystemId);
        if (scoreSystem.ClubId != clubId)
        {
            throw new InvalidOperationException("Score code is not for the current club.");
        }

        var coreObj = _mapper.Map<ScoreCode>(model);
        await _scoringService.SaveScoreCodeAsync(coreObj);

        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    public async Task<ActionResult> Edit(
        string clubInitials,
        Guid id,
        string returnUrl = null)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.IsUserClubAdministrator(User, clubId))
        {
            return Unauthorized();
        }

        var scoreCode = await _scoringService.GetScoreCodeAsync(id);

        if (scoreCode == null)
        {
            return new NotFoundResult();
        }
        ViewData["ReturnUrl"] = returnUrl;

        var vm = _mapper.Map<ScoreCodeWithOptionsViewModel>(scoreCode);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(
        string clubInitials,
        ScoreCodeWithOptionsViewModel model,
        string returnUrl = null)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.IsUserClubAdministrator(User, clubId))
        {
            return Unauthorized();
        }
        var scoreSystem = await _scoringService.GetScoringSystemAsync(model.ScoringSystemId);
        if (scoreSystem.ClubId != clubId)
        {
            throw new InvalidOperationException("Score code is not for current club.");
        }

        var coreObj = _mapper.Map<ScoreCode>(model);
        await _scoringService.SaveScoreCodeAsync(coreObj);

        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    public async Task<ActionResult> Delete(
        string clubInitials,
        Guid id,
        string returnUrl = null)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.IsUserClubAdministrator(User, clubId))
        {
            return Unauthorized();
        }

        var scoreCode = await _scoringService.GetScoreCodeAsync(id);

        if (scoreCode == null)
        {
            return new NotFoundResult();
        }
        ViewData["ReturnUrl"] = returnUrl;

        var vm = _mapper.Map<ScoreCodeWithOptionsViewModel>(scoreCode);
        return View(vm);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> PostDelete(
        string clubInitials,
        Guid id,
        Guid scoringSystemId,
        string returnUrl)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.IsUserClubAdministrator(User, clubId))
        {
            return Unauthorized();
        }
        var scoreSystem = await _scoringService.GetScoringSystemAsync(scoringSystemId);
        if (scoreSystem.ClubId != clubId)
        {
            throw new InvalidOperationException("Score code is not for current club.");
        }
        if (!(scoreSystem.ScoreCodes.Any(s => s.Id == id)))
        {
            throw new InvalidOperationException("Score code is not for current scoring system.");
        }
            
        await _scoringService.DeleteScoreCodeAsync(id);

        return Redirect(returnUrl);
    }
}
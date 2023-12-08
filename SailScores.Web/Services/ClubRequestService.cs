using Microsoft.Extensions.Configuration;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using Microsoft.Extensions.Caching.Memory;
using SailScores.Web.Services.Interfaces;
using IClubRequestService = SailScores.Web.Services.Interfaces.IClubRequestService;

namespace SailScores.Web.Services;

public class ClubRequestService : IClubRequestService
{
    private readonly Core.Services.IClubService _coreClubService;
    private readonly Core.Services.IClubRequestService _coreClubRequestService;
    private readonly Core.Services.IScoringService _coreScoringService;
    private readonly IUserService _coreUserService;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _memoryCache;
    private readonly IMapper _mapper;


    private const string clubCacheKeyName = "CachedClubList";

    public ClubRequestService(
        Core.Services.IClubService clubService,
        Core.Services.IClubRequestService clubRequestService,
        Core.Services.IScoringService scoringService,
        Core.Services.IUserService userService,
        IEmailSender emailSender,
        IConfiguration configuration,
        IMemoryCache memoryCache,
        IMapper mapper)
    {
        _coreClubService = clubService;
        _coreClubRequestService = clubRequestService;
        _coreScoringService = scoringService;
        _coreUserService = userService;
        _emailSender = emailSender;
        _configuration = configuration;
        _memoryCache = memoryCache;
        _mapper = mapper;
    }

    public async Task SubmitRequest(ClubRequestViewModel request)
    {
        var coreRequest = _mapper.Map<Core.Model.ClubRequest>(request);
        coreRequest.RequestSubmitted = DateTime.Now;
        coreRequest.ClubInitials = request.ClubInitials.ToUpper();
        var id = await _coreClubRequestService.Submit(coreRequest);

        await ProcessRequest(id, false, null);

        await SendUserNotice(request);
        await SendAdminNotice(coreRequest);
    }

    private async Task SendUserNotice(ClubRequestViewModel request)
    {
        var emailBody = await _emailSender.GetHtmlFromView("Templates/ClubCreated", request);
        await _emailSender.SendEmailAsync(request.ContactEmail, "SailScores Club Created", emailBody);
    }

    public async Task UpdateRequest(ClubRequestViewModel vm)
    {
        var coreRequest = _mapper.Map<Core.Model.ClubRequest>(vm);
        await _coreClubRequestService.UpdateRequest(coreRequest);
    }

    public async Task<IList<ClubRequestViewModel>> GetPendingRequests()
    {
        var requestList = await _coreClubRequestService.GetPendingRequests();

        return _mapper.Map<IList<ClubRequestViewModel>>(requestList);
    }

    public async Task<ClubRequestWithOptionsViewModel> GetRequest(Guid id)
    {
        var request = await _coreClubRequestService.GetRequest(id);

        var vm = _mapper.Map<ClubRequestWithOptionsViewModel>(request);
        vm.ClubOptions = _mapper.Map<IList<ClubSummaryViewModel>>(await _coreClubService.GetClubs(true));
        return vm;
    }

    public async Task<bool> AreInitialsAllowed(string initials)
    {
        if (!AreInitialsBasicAllowed(initials))
        {
            return false;
        }
        if(await AreInitialsInUse(initials))
        {
            return false;
        }
            
        var disallowedInitials = new List<string>
        {
            "TEST",
            "HOME",
            "ACCOUNT",
            "CLIENT",
            "API",
            "APIV2",
            "APIV3",

            "CSS",
            "VENDOR",
            "FONTS",
            "JS",
            "LIB",
            "FAVICON",
            "IMAGES",
            "IMAGE",
            "CONTENT",
            "SCRIPTS",
            "STATS",
            "SAILSCORES",

            "CLUB",
            "CLUBS",
            "TEAM",
            "REGATTA",
            "SERIES",
            "RACE",
            // Should obscenities be added here?
        };

        if (disallowedInitials.Contains(initials.ToUpperInvariant())){
            return false;
        }

        // made it through all the checks.
        return true;
    }
        
    public async Task ProcessRequest(
        Guid id,
        bool test,
        Guid? copyFromClubId)
    {
        var request = await _coreClubRequestService.GetRequest(id);

        request.ClubInitials = request.ClubInitials.ToUpperInvariant();

        Guid newClubId;

        if (copyFromClubId.HasValue && copyFromClubId.Value != Guid.Empty)
        {
            newClubId = await CopyClub(copyFromClubId.Value, request, test);
            request.TestClubId = newClubId;
            request.RequestApproved ??= DateTime.UtcNow;
            await _coreClubRequestService.UpdateRequest(request);
        }
        else
        {
            var baseScoringSystem = await _coreScoringService.GetSiteDefaultSystemAsync();

            ScoringSystem newScoringSystem = new ScoringSystem
            {
                ParentSystemId = baseScoringSystem.Id,
                Name = $"{request.ClubInitials} scoring based on App. A Rule 5.3",
                DiscardPattern = "0,1"
            };

            var initialsToUse = request.ClubInitials + (test ? "TEST" : "");
            var club = new Core.Model.Club
            {
                Id = Guid.Empty,
                Name = request.ClubName,
                Initials = initialsToUse,
                IsHidden = true,
                Url = request.ClubWebsite,
                DefaultScoringSystem = newScoringSystem,
                Description = (String.IsNullOrWhiteSpace(request.ClubLocation) ? (string)null : "_" + request.ClubLocation + "_"),
                ScoringSystems = new List<ScoringSystem> { newScoringSystem },
                Locale = "en-US"
            };

            newClubId = await _coreClubService.SaveNewClub(club);
            ClearClubMemoryCache();

            if (club.IsHidden)
            {
                request.TestClubId = newClubId;

            }
            else
            {
                request.VisibleClubId = newClubId;
            }
            request.RequestApproved ??= DateTime.UtcNow;
            await _coreClubRequestService.UpdateRequest(request);
        }

#pragma warning disable CA1308 // Normalize strings to uppercase
        // we are storing email in lowercase, and not round-tripping back to upper case.
        await _coreUserService.AddPermission(newClubId, request.ContactEmail.ToLowerInvariant());
#pragma warning restore CA1308 // Normalize strings to uppercase
    }

    private void ClearClubMemoryCache()
    {
        _memoryCache.Remove(clubCacheKeyName);
    }


    private async Task SendAdminNotice(ClubRequest request)
    {
        try
        {
            var emailBody = await _emailSender.GetHtmlFromView("Templates/ClubCreatedAdminNotice", request);
            var notificationEmail = _configuration["NotificationEmail"];
            await _emailSender.SendEmailAsync(notificationEmail, "ADMIN: SailScores Club Created", emailBody);
        }
        catch (Exception)
        {
            // if email is not sent, should add some alert for site admins, that there are 
            // requests to approve.
        }
    }

    private async Task<Guid> CopyClub(Guid copyFromClubId,
        ClubRequest request,
        bool test)
    {
        var targetClub = new Core.Model.Club
        {
            Name = request.ClubName,
            Initials = request.ClubInitials + (test ? "TEST" : ""),
            IsHidden = test,
            Url = request.ClubWebsite,
        };
        return await _coreClubService.CopyClubAsync(
            copyFromClubId,
            targetClub);
    }

    private bool AreInitialsBasicAllowed(string initials)
    {

        return initials != null
               && initials.Length > 2
               && AllValidCharacters(initials);
    }

    private bool AllValidCharacters(string initials)
    {
        foreach (char c in initials)
        {
            if(!char.IsLetterOrDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> AreInitialsInUse(string initials)
    {
        var clubInitials = (await _coreClubService.GetClubs(true).ConfigureAwait(false))
            .Select(c => c.Initials.ToUpperInvariant());

        return clubInitials.Contains(initials.ToUpperInvariant());
    }

}
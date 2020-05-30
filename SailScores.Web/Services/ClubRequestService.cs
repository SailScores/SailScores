using AutoMapper;
using Microsoft.Extensions.Configuration;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public class ClubRequestService : IClubRequestService
    {
        private readonly Core.Services.IClubService _coreClubService;
        private readonly Core.Services.IClubRequestService _coreClubRequestService;
        private readonly Core.Services.IScoringService _coreScoringService;
        private readonly IUserService _coreUserService;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;


        public ClubRequestService(
            Core.Services.IClubService clubService,
            Core.Services.IClubRequestService clubRequestService,
            Core.Services.IScoringService scoringService,
            Core.Services.IUserService userService,
            IEmailSender emailSender,
            IConfiguration configuration,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _coreClubRequestService = clubRequestService;
            _coreScoringService = scoringService;
            _coreUserService = userService;
            _emailSender = emailSender;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task SubmitRequest(ClubRequestViewModel request)
        {
            var coreRequest = _mapper.Map<Core.Model.ClubRequest>(request);
            coreRequest.RequestSubmitted = DateTime.Now;
            await _coreClubRequestService.Submit(coreRequest);
            var notificationEmail = _configuration["NotificationEmail"];
            try {
                await _emailSender.SendEmailAsync(notificationEmail, "SailScores - Club Request submitted.",
                       $"A club has been requested for {request.ClubName} by {request.ContactEmail}.");

            }
            catch (Exception)
            {
                // if email is not sent , should add some alert for site admins, that there are 
                // requests to approve.
            }
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
            vm.ClubOptions = _mapper.Map<IList<ClubSummaryViewModel>>( await _coreClubService.GetClubs(true));
            return vm;
        }

        public async Task ProcessRequest(
            Guid id,
            bool test,
            Guid? copyFromClubId)
        {
            var request = await _coreClubRequestService.GetRequest(id);

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
                    Name = $"{request.ClubName} scoring based on App. A Low Point",
                    DiscardPattern = "0,1"
                };

                var initialsToUse = request.ClubInitials + (test ? "TEST" : "");
                var club = new Core.Model.Club
                {
                    Id = Guid.Empty,
                    Name = request.ClubName,
                    Initials = initialsToUse,
                    IsHidden = test,
                    Url = request.ClubWebsite,
                    DefaultScoringSystem = newScoringSystem,
                    Description = (String.IsNullOrWhiteSpace(request.ClubLocation) ? (string)null : "_"+request.ClubLocation+"_"),
                    ScoringSystems = new List<ScoringSystem> { newScoringSystem }
                };

                newClubId = await _coreClubService.SaveNewClub(club);

                if(test)
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
            await _coreUserService.AddPermision(newClubId, request.ContactEmail.ToLowerInvariant());
#pragma warning restore CA1308 // Normalize strings to uppercase
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

    }
}

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Dtos;
using SailScores.Core.Model;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    public class FleetsController : ControllerBase
    {
        // IdentityConstants.ApplicationScheme's value ("Identity.Application") hardcoded here because
        // attribute arguments must be compile-time constants and ApplicationScheme is a static readonly field.
        private const string CookieOrJwtSchemes =
            "Identity.Application," + JwtBearerDefaults.AuthenticationScheme;

        private readonly CoreServices.IClubService _clubService;
        private readonly CoreServices.IFleetService _coreFleetService;
        private readonly CoreServices.ICompetitorService _coreCompetitorService;
        private readonly IAuthorizationService _authService;
        private readonly IMapper _mapper;

        public FleetsController(
            CoreServices.IClubService clubService,
            CoreServices.IFleetService coreFleetService,
            CoreServices.ICompetitorService coreCompetitorService,
            IAuthorizationService authService,
            IMapper mapper)
        {
            _clubService = clubService;
            _coreFleetService = coreFleetService;
            _coreCompetitorService = coreCompetitorService;
            _authService = authService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IEnumerable<FleetDto>> Get(Guid clubId)
        {
            var fleets = await _clubService.GetAllFleets(clubId);
            return _mapper.Map<List<FleetDto>>(fleets);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Post([FromBody] FleetDto fleet)
        {
            if (!await _authService.CanUserEdit(User, fleet.ClubId))
            {
                return Unauthorized();
            }
            var fleetBizObj = _mapper.Map<Fleet>(fleet);
            await _clubService.SaveNewFleet(fleetBizObj);
            var savedFleet =
                (await _clubService.GetAllFleets(fleet.ClubId))
                .First(c => c.Name == fleet.Name);
            return Ok(savedFleet.Id);
        }

        [HttpPost("{fleetId}/competitors/{competitorId}")]
        [Authorize(AuthenticationSchemes = CookieOrJwtSchemes)]
        public async Task<IActionResult> AddCompetitor(Guid fleetId, Guid competitorId)
            => await ToggleMembership(fleetId, competitorId, add: true);

        [HttpDelete("{fleetId}/competitors/{competitorId}")]
        [Authorize(AuthenticationSchemes = CookieOrJwtSchemes)]
        public async Task<IActionResult> RemoveCompetitor(Guid fleetId, Guid competitorId)
            => await ToggleMembership(fleetId, competitorId, add: false);

        private async Task<IActionResult> ToggleMembership(Guid fleetId, Guid competitorId, bool add)
        {
            var fleet = await _coreFleetService.Get(fleetId);
            if (fleet == null)
            {
                return NotFound();
            }

            var isAdmin = await _authService.IsUserClubAdministrator(User, fleet.ClubId)
                || await _authService.IsUserFullAdmin(User);
            if (!isAdmin)
            {
                return Unauthorized();
            }

            if (fleet.FleetType != SailScores.Api.Enumerations.FleetType.SelectedBoats)
            {
                return BadRequest("Fleet membership can only be edited manually for fleets of type Selected Boats.");
            }

            var competitor = await _coreCompetitorService.GetCompetitorAsync(competitorId);
            if (competitor == null || competitor.ClubId != fleet.ClubId)
            {
                return NotFound();
            }

            if (add)
            {
                await _coreFleetService.AddCompetitorToFleet(fleetId, competitorId);
            }
            else
            {
                await _coreFleetService.RemoveCompetitorFromFleet(fleetId, competitorId);
            }

            return Ok();
        }
    }
}

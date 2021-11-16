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
        private readonly CoreServices.IClubService _clubService;
        private readonly IAuthorizationService _authService;
        private readonly IMapper _mapper;

        public FleetsController(
            CoreServices.IClubService clubService,
            IAuthorizationService authService,
            IMapper mapper)
        {
            _clubService = clubService;
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

    }
}

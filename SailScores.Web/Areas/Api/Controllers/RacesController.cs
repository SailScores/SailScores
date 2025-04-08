using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Dtos;
using SailScores.Web.Models.SailScores;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class RacesController : ControllerBase
    {
        private readonly Core.Services.IRaceService _service;
        private readonly IAuthorizationService _authService;
        private readonly IMapper _mapper;

        public RacesController(
            Core.Services.IRaceService service,
            IAuthorizationService authService,
            IMapper mapper)
        {
            _service = service;
            _authService = authService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IEnumerable<RaceDto>> Get(Guid clubId)
        {
            var races = await _service.GetRacesAsync(clubId);
            return _mapper.Map<List<RaceDto>>(races);
        }

        [HttpGet("{identifier}")]
        public async Task<RaceDto> Get([FromRoute] String identifier)
        {
            var r = await _service.GetRaceAsync(Guid.Parse(identifier));

            return _mapper.Map<RaceDto>(r);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Post([FromBody] RaceDto race)
        {
            if (!await _authService.CanUserEdit(User, race.ClubId))
            {
                return Unauthorized();
            }
            return Ok(await _service.SaveAsync(race));
        }


        [AllowAnonymous]
        [HttpGet("racenumber")]
        public async Task<RaceNumberViewModel> GetRaceNumber(
            Guid clubId,
            Guid fleetId,
            DateTime raceDate,
            Guid? regattaId = null)
        {
            int raceNumber = await _service.GetNewRaceNumberAsync(
                clubId,
                fleetId,
                raceDate,
                regattaId);

            return new RaceNumberViewModel
            {
                Order = raceNumber,
                Date = raceDate,
                Fleet = fleetId
            };
        }
    }
}

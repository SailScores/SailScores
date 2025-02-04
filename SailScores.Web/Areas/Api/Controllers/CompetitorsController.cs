using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Api.Dtos;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class CompetitorsController : ControllerBase
    {
        private readonly CoreServices.ICompetitorService _service;
        private readonly IAuthorizationService _authService;
        private readonly IMapper _mapper;

        public CompetitorsController(
            CoreServices.ICompetitorService service,
            IAuthorizationService authService,
            IMapper mapper)
        {
            _service = service;
            _authService = authService;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IEnumerable<CompetitorDto>> Get(
            Guid clubId,
            Guid? fleetId,
            bool includeInactive = false)
        {
            var competitors = await _service.GetCompetitorsAsync(clubId, fleetId, includeInactive);
            return _mapper.Map<List<CompetitorDto>>(competitors);
        }

        [HttpGet("{identifier}")]
        public async Task<CompetitorDto> Get([FromRoute] String identifier)
        {
            var c = await _service.GetCompetitorAsync(Guid.Parse(identifier));

            return _mapper.Map<CompetitorDto>(c);
        }

        // POST: api/competitors
        [HttpPost]
        public async Task<ActionResult<Guid>> Post([FromBody] CompetitorDto competitor)
        {
            if (!await _authService.CanUserEdit(User, competitor.ClubId))
            {
                return Unauthorized();
            }
            await _service.SaveAsync(competitor);

            var savedCompetitor =
                (await _service.GetCompetitorsAsync(competitor.ClubId, null, true))
                .First(c => c.Name == competitor.Name
                && c.SailNumber == competitor.SailNumber
                && c.AlternativeSailNumber == competitor.AlternativeSailNumber);
            return Ok(savedCompetitor.Id);
        }

        // PUT: api/competitors/5
        [HttpPut("{id}")]
        public async Task<ActionResult> Put(Guid id, [FromBody] CompetitorDto value)
        {
            if (!await _authService.CanUserEdit(User, value.ClubId))
            {
                return Unauthorized();
            }
            Competitor comp = _mapper.Map<Competitor>(value);
            comp.Id = id;
            await _service.SaveAsync(comp);
            return Ok();
        }

    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Dtos;

namespace SailScores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class RacesController : ControllerBase
    {
        private readonly Core.Services.IRaceService _service;
        private readonly Services.IAuthorizationService _authService;
        private readonly IMapper _mapper;

        public RacesController(
            Core.Services.IRaceService service,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _service = service;
            _authService = authService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IEnumerable<RaceDto>> Get(Guid clubId)
        {
            var races =  await _service.GetRacesAsync(clubId);
            return _mapper.Map<List<RaceDto>>(races);
        }

        [HttpGet("{identifier}")]
        public async Task<RaceDto> Get([FromRoute] String identifier)
        {
            var r = await _service.GetRaceAsync(Guid.Parse(identifier));

            return _mapper.Map<RaceDto>(r); ;
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Post([FromBody] RaceDto race)
        {
            if (!await _authService.CanUserEdit(User, race.ClubId))
            {
                return Unauthorized();
            }
            return Ok( await _service.SaveAsync(race));
        }

    }
}

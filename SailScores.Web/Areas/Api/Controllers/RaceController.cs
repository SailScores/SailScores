using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sailscores.Core.Model.Dto;
using Sailscores.Core.Services;
using Sailscores.Web.Services;
using Model = Sailscores.Core.Model;

namespace Sailscores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RaceController : ControllerBase
    {
        private readonly Core.Services.IRaceService _service;
        private readonly IMapper _mapper;

        public RaceController(
            Core.Services.IRaceService service,
            IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet]
        public async Task<IEnumerable<RaceDto>> Get(Guid clubId)
        {
            var races =  await _service.GetRacesAsync(clubId);
            return _mapper.Map<List<RaceDto>>(races);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("{identifier}")]
        public async Task<RaceDto> Get([FromRoute] String identifier)
        {
            var r = await _service.GetRaceAsync(Guid.Parse(identifier));

            return _mapper.Map<RaceDto>(r); ;
        }
        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Core.Model.Dto;
using SailScores.Core.Services;
using SailScores.Web.Services;
using Model = SailScores.Core.Model;

namespace SailScores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompetitorController : ControllerBase
    {
        private readonly ICompetitorService _service;
        private readonly IMapper _mapper;

        public CompetitorController(
            ICompetitorService service,
            IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet]
        public async Task<IEnumerable<CompetitorDto>> Get(Guid clubId)
        {
            var competitors =  await _service.GetCompetitorsAsync(clubId);
            return _mapper.Map<List<CompetitorDto>>(competitors);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("{identifier}")]
        public async Task<CompetitorDto> Get([FromRoute] String identifier)
        {
            var c = await _service.GetCompetitorAsync(Guid.Parse(identifier));

            return _mapper.Map<CompetitorDto>(c); ;
        }

        [Authorize]
        // POST: api/Club
        [HttpPost]
        public async Task Post([FromBody] CompetitorDto value)
        {
            Competitor comp = _mapper.Map<Competitor>(value);

            await _service.SaveAsync(comp);
        }

        [Authorize]
        // PUT: api/Club/5
        [HttpPut("{id}")]
        public async Task Put(Guid id, [FromBody] CompetitorDto value)
        {
            Competitor comp = _mapper.Map<Competitor>(value);
            comp.Id = id;
            await _service.SaveAsync(comp);
        }

    }
}

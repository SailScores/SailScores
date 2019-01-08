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
using SailScores.Api.Dtos;
using SailScores.Core.Services;
using SailScores.Web.Services;
using Model = SailScores.Core.Model;

namespace SailScores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class CompetitorsController : ControllerBase
    {
        private readonly ICompetitorService _service;
        private readonly IMapper _mapper;

        public CompetitorsController(
            ICompetitorService service,
            IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IEnumerable<CompetitorDto>> Get(
            Guid clubId,
            Guid? fleetId)
        {
            var competitors =  await _service.GetCompetitorsAsync(clubId, fleetId);
            return _mapper.Map<List<CompetitorDto>>(competitors);
        }

        [HttpGet("{identifier}")]
        public async Task<CompetitorDto> Get([FromRoute] String identifier)
        {
            var c = await _service.GetCompetitorAsync(Guid.Parse(identifier));

            return _mapper.Map<CompetitorDto>(c); ;
        }
        
        // POST: api/competitors
        [HttpPost]
        public async Task<Guid> Post([FromBody] CompetitorDto competitor)
        {
            
            await _service.SaveAsync(competitor);

            var savedCompetitor =
                (await _service.GetCompetitorsAsync(competitor.ClubId, null))
                .First(c => c.Name == competitor.Name
                && c.SailNumber == competitor.SailNumber
                && c.AlternativeSailNumber == competitor.AlternativeSailNumber);
            return savedCompetitor.Id;
        }

        // PUT: api/competitors/5
        [HttpPut("{id}")]
        public async Task Put(Guid id, [FromBody] CompetitorDto value)
        {
            Competitor comp = _mapper.Map<Competitor>(value);
            comp.Id = id;
            await _service.SaveAsync(comp);
        }

    }
}

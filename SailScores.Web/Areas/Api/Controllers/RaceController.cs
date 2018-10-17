using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Services;
using SailScores.Web.Areas.Api.Models;

namespace SailScores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RaceController : ControllerBase
    {
        private readonly IClubService _clubService;
        private readonly IMapper _mapper;

        public RaceController(
            IClubService clubService,
            IMapper mapper)
        {
            _clubService = clubService;
            _mapper = mapper;
        }

        // GET: api/Race
        [HttpGet]
        public async Task<IEnumerable<RaceViewModel>> Get([FromQuery] string clubIdentifier)
        {
            var club = await _clubService.GetFullClub(clubIdentifier);
            return _mapper.Map<List<RaceViewModel>>(club.Races);

        }

        // GET: api/Race/5
        [HttpGet("{id}")]
        public async Task<string> Get([FromRoute] Guid id)
        {
            return id.ToString();
        }

        // POST: api/Race
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT: api/Race/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

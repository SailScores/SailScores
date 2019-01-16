using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Dtos;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Services;
using Model = SailScores.Core.Model;

namespace SailScores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    public class FleetsController : ControllerBase
    {
        private readonly IClubService _clubService;
        private readonly IMapper _mapper;

        public FleetsController(
            IClubService clubService,
            IMapper mapper)
        {
            _clubService = clubService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IEnumerable<FleetDto>> Get(Guid clubId)
        {
            var fleets = await _clubService.GetAllFleets(clubId);
            return _mapper.Map<List<FleetDto>>(fleets);
        }

        [HttpPost]
        public async Task<Guid> Post([FromBody] FleetDto fleet)
        {
            var fleetBizObj = _mapper.Map<Fleet>(fleet);
            await _clubService.SaveNewFleet(fleetBizObj);
            var savedFleet =
                (await _clubService.GetFullClub(fleet.ClubId))
                .Fleets
                .First(c => c.Name == fleet.Name);
            return savedFleet.Id;
        }

        // PUT: api/Club/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }
    }
}

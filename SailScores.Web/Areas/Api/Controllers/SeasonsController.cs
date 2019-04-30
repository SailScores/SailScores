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
    public class SeasonsController : ControllerBase
    {
        private readonly Core.Services.IClubService _clubService;
        private readonly Services.IAuthorizationService _authService;
        private readonly IMapper _mapper;

        public SeasonsController(
            Core.Services.IClubService clubService,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _clubService = clubService;
            _authService = authService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IEnumerable<SeasonDto>> Get(Guid clubId)
        {
            var club = await _clubService.GetFullClub(clubId);
            return _mapper.Map<List<SeasonDto>>(club.Seasons);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Post([FromBody] SeasonDto season)
        {
            if (!await _authService.CanUserEdit(User, season.ClubId))
            {
                return Unauthorized();
            }
            var seasonBizObj = _mapper.Map<Season>(season);
            await _clubService.SaveNewSeason(seasonBizObj);
            var savedSeason =
                (await _clubService.GetFullClub(season.ClubId))
                .Seasons
                .First(c => c.Name == season.Name);
            return Ok(savedSeason.Id);
        }

    }
}

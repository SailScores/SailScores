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
    public class ClubController : ControllerBase
    {
        private readonly IClubService _clubService;
        private readonly IMapper _mapper;

        public ClubController(
            IClubService clubService,
            IMapper mapper)
        {
            _clubService = clubService;
            _mapper = mapper;
        }

        /// <summary>
        /// Get summary list of clubs: some properties may be empty.
        /// </summary>
        /// <returns>Array of Model.Club</returns>
        // GET: api/Club

        [HttpGet]
        public async Task<IEnumerable<ClubDto>> Get()
        {
            var clubs =  await _clubService.GetClubs(false);
            return _mapper.Map<List<ClubDto>>(clubs);
        }

        /// <summary>
        /// Retrieve details for a single club
        /// </summary>
        /// <param name="identifier">Initials or Guid for Club</param>
        /// <returns>Model.Club</returns>
        // GET: api/Club/5
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("{identifier}")]
        public async Task<ClubDto> Get([FromRoute] string identifier)
        {
            var club = await _clubService.GetFullClub(identifier);

            return _mapper.Map<ClubDto>(club);
        }

        [Authorize]
        // POST: api/Club
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        [Authorize]
        // PUT: api/Club/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }
    }
}

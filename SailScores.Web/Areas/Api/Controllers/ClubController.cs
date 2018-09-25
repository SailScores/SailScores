using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Services;
using Model = SailScores.Core.Model;

namespace SailScores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClubController : ControllerBase
    {
        private readonly IClubService _clubService;

        public ClubController(IClubService clubService)
        {
            _clubService = clubService;
        }

        /// <summary>
        /// Get summary list of clubs: some properties may be empty.
        /// </summary>
        /// <returns>Array of Model.Club</returns>
        // GET: api/Club
        [HttpGet]
        public async Task<IEnumerable<Model.Club>> Get()
        {
            return await _clubService.GetClubs(false);
        }

        /// <summary>
        /// Retrieve details for a single club
        /// </summary>
        /// <param name="identifier">Initials or Guid for Club</param>
        /// <returns>Model.Club</returns>
        // GET: api/Club/5
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("{identifier}")]
        public async Task<Model.Club> Get([FromRoute] string identifier)
        {
            return await _clubService.GetFullClub(identifier);
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

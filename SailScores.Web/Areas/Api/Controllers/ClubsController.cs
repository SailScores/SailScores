using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Dtos;
using SailScores.Core.Model;

namespace SailScores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ClubsController : ControllerBase
    {
        private readonly Core.Services.IClubService _clubService;
        private readonly Services.IAuthorizationService _authService;
        private readonly IMapper _mapper;

        public ClubsController(
            Core.Services.IClubService clubService,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _clubService = clubService;
            _authService = authService;
            _mapper = mapper;
        }

        /// <summary>
        /// Get summary list of clubs: some properties may be empty.
        /// </summary>
        /// <returns>Array of Model.Club</returns>
        // GET: api/Clubs

        [HttpGet]
        public async Task<IEnumerable<ClubDto>> Get()
        {
            var clubs = await _clubService.GetClubs(true);

            var filteredClubs = new List<Club>();
            foreach (var club in clubs)
            {
                if (await _authService.CanUserEdit(User, club.Id))
                {
                    filteredClubs.Add(club);
                }
            }
            return _mapper.Map<List<ClubDto>>(filteredClubs);
        }

        /// <summary>
        /// Retrieve details for a single club
        /// </summary>
        /// <param name="identifier">Initials or Guid for Club</param>
        /// <returns>Model.Club</returns>
        // GET: api/Club/5
        [HttpGet("{identifier}")]
        public async Task<ClubDto> Get([FromRoute] string identifier)
        {
            var club = await _clubService.GetFullClubExceptScores(identifier);

            return _mapper.Map<ClubDto>(club);
        }

        // POST: api/Club
        [HttpPost]
        public async Task<ActionResult<Guid>> Post([FromBody] ClubDto club)
        {
            if (club == null)
            {
                throw new ArgumentNullException(nameof(club));
            }
            // special handling here, so that user can create new club if they have
            // global permissions.
            bool canEdit = false;
            if (club.Id == default)
            {
                canEdit = await _authService.CanUserEdit(User, club.Initials);
            }
            else
            {
                canEdit = await _authService.CanUserEdit(User, club.Id);
            }
            if (!canEdit)
            {
                return Unauthorized();
            }

            var clubBizObj = _mapper.Map<Club>(club);
            await _clubService.SaveNewClub(clubBizObj);
            var savedClub = (await _clubService.GetClubs(false)).First(c => c.Initials == club.Initials);
            return Ok(savedClub.Id);
        }

    }
}

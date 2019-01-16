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
    public class BoatClassesController : ControllerBase
    {
        private readonly IClubService _clubService;
        private readonly IMapper _mapper;

        public BoatClassesController(
            IClubService clubService,
            IMapper mapper)
        {
            _clubService = clubService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IEnumerable<BoatClassDto>> Get(Guid clubId)
        {
            var club = await _clubService.GetFullClub(clubId);
            return _mapper.Map<List<BoatClassDto>>(club.BoatClasses);
        }

        [HttpPost]
        public async Task<Guid> Post([FromBody] BoatClassDto boatClass)
        {
            var classBizObj = _mapper.Map<BoatClass>(boatClass);
            await _clubService.SaveNewBoatClass(classBizObj);
            var savedClass =
                (await _clubService.GetFullClub(boatClass.ClubId))
                .BoatClasses
                .First(c => c.Name == boatClass.Name);
            return savedClass.Id;
        }

        // PUT: api/Club/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Dtos;
using SailScores.Core.Services;

namespace SailScores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ScoreCodesController : ControllerBase
    {
        private readonly IScoringService _service;
        private readonly IMapper _mapper;

        public ScoreCodesController(
            IScoringService service,
            IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IEnumerable<ScoreCodeDto>> Get(Guid clubId)
        {
            var scoreCodes = await _service.GetScoreCodesAsync(clubId);
            return _mapper.Map<List<ScoreCodeDto>>(scoreCodes);
        }

    }
}

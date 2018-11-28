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
    public class ScoreCodeController : ControllerBase
    {
        private readonly IScoringService _service;
        private readonly IMapper _mapper;

        public ScoreCodeController(
            IScoringService service,
            IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet]
        public async Task<IEnumerable<ScoreCodeDto>> Get(Guid clubId)
        {
            var scoreCodes =  await _service.GetScoreCodesAsync(clubId);
            return _mapper.Map<List<ScoreCodeDto>>(scoreCodes);
        }
                
    }
}

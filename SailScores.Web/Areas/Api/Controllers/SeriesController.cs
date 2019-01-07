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
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SeriesController : ControllerBase
    {
        private readonly Core.Services.ISeriesService _service;
        private readonly IMapper _mapper;

        public SeriesController(
            Core.Services.ISeriesService service,
            IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IEnumerable<SeriesDto>> Get(Guid clubId)
        {
            var competitors =  await _service.GetAllSeriesAsync(clubId);
            return _mapper.Map<List<SeriesDto>>(competitors);
        }

        [HttpGet("{identifier}")]
        public async Task<SeriesDto> Get([FromRoute] String identifier)
        {
            var c = await _service.GetOneSeriesAsync(Guid.Parse(identifier));

            return _mapper.Map<SeriesDto>(c); ;
        }

        [HttpPost]
        public async Task<Guid> Post([FromBody] SeriesDto series)
        {
            var seriesBizObj = _mapper.Map<Series>(series);
            await _service.SaveNewSeries(seriesBizObj);
            var savedSeries =
                (await _service.GetAllSeriesAsync(series.ClubId))                
                .First(s => s.Name == series.Name
                    && s.Season.Id == series.SeasonId);
            return savedSeries.Id;
        }
    }
}

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Dtos;
using SailScores.Core.Model;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Areas.Api.Controllers
{
    public class SeriesListResult
    {
        public required IEnumerable<SeriesDto> Series { get; set; }
        public bool NoSeasonForDate { get; set; }
        public bool NoSeriesForDate { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SeriesController : ControllerBase
    {
        private readonly Core.Services.ISeriesService _service;
        private readonly Core.Services.ISeasonService _seasonService;
        private readonly IAuthorizationService _authService;
        private readonly IMapper _mapper;

        public SeriesController(
            Core.Services.ISeriesService service,
            Core.Services.ISeasonService seasonService,
            IAuthorizationService authService,
            IMapper mapper)
        {
            _service = service;
            _seasonService = seasonService;
            _authService = authService;
            _mapper = mapper;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<SeriesListResult>> Get(
            Guid clubId,
            DateTime? date,
            bool? includeSummary = true
            )
        {
            var series = await _service.GetAllSeriesAsync(clubId, date, true, includeSummary ?? true);
            var noSeasonForDate = false;
            var noSeriesForDate = false;

            if (date.HasValue)
            {
                var seasons = await _seasonService.GetSeasons(clubId);
                noSeasonForDate = !seasons.Any(s => s.Start <= date.Value && s.End >= date.Value);
                noSeriesForDate = !series.Any();
            }

            return Ok(new SeriesListResult
            {
                Series = _mapper.Map<List<SeriesDto>>(series),
                NoSeasonForDate = noSeasonForDate,
                NoSeriesForDate = noSeriesForDate
            });
        }

        [HttpGet("{identifier}")]
        public async Task<SeriesDto> Get([FromRoute] String identifier)
        {
            var c = await _service.GetOneSeriesAsync(Guid.Parse(identifier));

            return _mapper.Map<SeriesDto>(c);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Post([FromBody] SeriesDto series)
        {
            if (!await _authService.CanUserEdit(User, series.ClubId))
            {
                return Unauthorized();
            }
            var seriesBizObj = _mapper.Map<Series>(series);
            await _service.SaveNewSeries(seriesBizObj);
            var savedSeries =
                (await _service.GetAllSeriesAsync(series.ClubId, null, true, true))
                .Single(s => s.Name == series.Name
                    && s.Season.Id == series.SeasonId);
            return Ok(savedSeries.Id);
        }

        /// <summary>
        /// Returns summary series for a given club and season. Intended for UI option lists.
        /// </summary>
        [HttpGet("summary")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<object>>> GetSummarySeries(
            [FromQuery] Guid clubId,
            [FromQuery] Guid seasonId)
        {
            if (clubId == Guid.Empty || seasonId == Guid.Empty)
            {
                return BadRequest();
            }

            var all = await _service.GetAllSeriesAsync(clubId, null, true, true);
            var summaries = all
                .Where(s => s.Type == SeriesType.Summary)
                .Where(s => s.Season != null && s.Season.Id == seasonId)
                .OrderBy(s => s.Name)
                .Select(s => new { id = s.Id, name = s.Name });

            return Ok(summaries);
        }
    }
}

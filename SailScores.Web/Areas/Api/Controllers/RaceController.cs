using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SailScores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RaceController : ControllerBase
    {
        // GET: api/Race
        [HttpGet]
        public IEnumerable<string> Get([FromQuery] string clubIdentifier)
        {
            return new string[] { "value1" + clubIdentifier, "value2" };
        }

        // GET: api/Race/5
        [HttpGet("{id}")]
        public async Task<string> Get([FromRoute] Guid id)
        {
            return id.ToString();
        }

        // POST: api/Race
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT: api/Race/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

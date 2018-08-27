using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SailScores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClubController : ControllerBase
    {
        // GET: api/Club
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "Club1", "Club2" };
        }

        [Authorize]
        // GET: api/Club/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            return "Club" + id;
        }

        // POST: api/Club
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT: api/Club/5
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;
using CoreServices = SailScores.Core.Services;
using SailScores.Web.Models;
using SailScores.Web.Services;

namespace SailScores.Web.Controllers
{
    public class ErrorController : Controller
    {
        
        public ErrorController()
        {
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("error/404")]
        public IActionResult Error404()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("error/{code:int}")]
        public IActionResult Error(int code)
        {
            // handle different codes or just return the default error view
            return View();
        }
    }
}
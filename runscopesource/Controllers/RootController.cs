using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace runscopesource.Controllers
{
    [Produces("application/json")]
    [Route("")]
    public class RootController : Controller
    {
        [HttpGet]
        [HttpPost]
        public string Get([FromBody]string value)
        {
            return string.Empty;
        }
    }
}

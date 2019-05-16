using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdminPanel.Entity;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // POST api/values
        [HttpPost]
        public void StartServer([FromBody] string value)
        {
            Program.servers.Add(new Server
            {

            });
        }
    }
}

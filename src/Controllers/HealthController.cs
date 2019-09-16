using Microsoft.AspNetCore.Mvc;
using Lybecker.K8sFriendlyAspNetCore.Models;

namespace Lybecker.K8sFriendlyAspNetCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        // If the process ready to serve requests e.g. done warmup of cache etc.
        [HttpGet]
        [Route("ready")]
        public IActionResult Ready()
        {
            return Ok(new HealthEndpointResponse
            { 
                Message = "Yes. Ready now."
            });
        }

        // Check that the ASP.NET runtime responds
        [HttpGet]
        public IActionResult Alive()
        {
            return Ok(new HealthEndpointResponse
            { 
                Message = "Alive. All is well."
            });
        }
    }
}
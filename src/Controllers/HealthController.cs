using Microsoft.AspNetCore.Mvc;

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
            return Ok("Yes, ready now");
        }

        // Check that the ASP.NET runtime responds
        [HttpGet]
        public IActionResult Alive()
        {
            return Ok("Alive. All is well.");
        }
    }
}
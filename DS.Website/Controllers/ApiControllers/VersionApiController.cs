using DS.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Route("/api/version")]
    public class VersionApiController : Controller
    {
        [HttpGet]
        public IActionResult GetVersion()
        {
            return Ok(new VersionDto
            {
                Version = DSVersion.Version,
                Name = DSVersion.Name
            });
        }
    }
}

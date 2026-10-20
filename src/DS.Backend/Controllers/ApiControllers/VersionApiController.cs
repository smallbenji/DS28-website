using DS.Domain;
using DS.Domain.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers.ApiControllers
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

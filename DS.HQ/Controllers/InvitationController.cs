using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.HQ.Controllers
{
    [AllowAnonymous]
    [Route("/invitation")]
    public class InvitationController(IWebHostEnvironment env) : Controller
    {
        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult Index()
        {
                var filePath = Path.Combine(env.ContentRootPath, "wwwroot", "dist", "index.html");

            return PhysicalFile(filePath, "text/html");
        }
    }
}
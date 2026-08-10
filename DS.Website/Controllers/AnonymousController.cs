using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [AllowAnonymous]
    public class AnonymousController(IWebHostEnvironment env) : Controller
    {
        [HttpGet("/login")]
        public IActionResult Login() => SpaIndex();

        [HttpGet("/register")]
        public IActionResult Register() => SpaIndex();

        [HttpGet("/invitation/{id}")]
        public IActionResult Invitation() => SpaIndex();

        [HttpGet("/reset-password/{id}")]
        public IActionResult ResetPassword() => SpaIndex();

        private IActionResult SpaIndex()
        {
            var filePath = Path.Combine(env.ContentRootPath, "wwwroot", "dist", "index.html");

            return PhysicalFile(filePath, "text/html");
        }
    }
}

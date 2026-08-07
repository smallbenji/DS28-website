using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [AllowAnonymous]
    public class AuthPageController(IWebHostEnvironment env) : Controller
    {
        [HttpGet("/login")]
        public IActionResult Login() => SpaIndex();

        [HttpGet("/register")]
        public IActionResult Register() => SpaIndex();

        private IActionResult SpaIndex()
        {
            var filePath = Path.Combine(env.ContentRootPath, "wwwroot", "dist", "index.html");

            return PhysicalFile(filePath, "text/html");
        }
    }
}

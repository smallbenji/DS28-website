using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers
{
    [AllowAnonymous]
    public class AnonymousController(IWebHostEnvironment env) : Controller
    {
        [HttpGet("/login")]
        public IActionResult Login() => SpaIndex();

        [HttpGet("/register")]
        public IActionResult Register() => SpaIndex();

        [HttpGet("/group-pre-signup")]
        public IActionResult GroupPreSignup() => SpaIndex();

        [HttpGet("/group-invitation/{id}")]
        public IActionResult GroupInvitation() => SpaIndex();

        [HttpGet("/invitation/{id}")]
        public IActionResult Invitation() => SpaIndex();

        [HttpGet("/reset-password/{id}")]
        public IActionResult ResetPassword() => SpaIndex();

        [HttpGet("/forgot-password")]
        public IActionResult ForgotPassword() => SpaIndex();

        [HttpGet("/beta/login")]
        public IActionResult BetaLogin() => BetaIndex();

        [HttpGet("/beta/register")]
        public IActionResult BetaRegister() => BetaIndex();

        [HttpGet("/beta/group-pre-signup")]
        public IActionResult BetaGroupPreSignup() => BetaIndex();

        [HttpGet("/beta/group-invitation/{id}")]
        public IActionResult BetaGroupInvitation() => BetaIndex();

        [HttpGet("/beta/invitation/{id}")]
        public IActionResult BetaInvitation() => BetaIndex();

        [HttpGet("/beta/reset-password/{id}")]
        public IActionResult BetaResetPassword() => BetaIndex();

        [HttpGet("/beta/forgot-password")]
        public IActionResult BetaForgotPassword() => BetaIndex();

        private IActionResult SpaIndex()
        {
            var filePath = Path.Combine(env.ContentRootPath, "wwwroot", "dist", "index.html");

            return PhysicalFile(filePath, "text/html");
        }

        private IActionResult BetaIndex()
        {
            var filePath = Path.Combine(env.ContentRootPath, "wwwroot", "dist-beta", "index.html");

            return PhysicalFile(filePath, "text/html");
        }
    }
}

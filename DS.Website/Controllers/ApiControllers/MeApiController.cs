using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Route("/api/v1/me")]
    public class MeApiController(UserManager<User> userManager) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            var model = new MeDTO
            {
                Name = user.GetFullName()
            };

            return Ok(model);
        }
    }

    public class MeDTO
    {
        public string Name { get; set; }
    }
}
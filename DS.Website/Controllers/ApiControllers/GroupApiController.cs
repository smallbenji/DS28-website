using DS.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Controllers;

[Route("/api/v1/group")]
// [Authorize(Roles = nameof(AppRoles.AuditLogView))]
public class GroupApiController(UserManager<User> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(HttpContext.User);
        var user = await userManager.Users
            .Include(x => x.Group)
                .ThenInclude(x => x.Patrols)
            .Include(x => x.Group)
                .ThenInclude(x => x.Scouts)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return Ok(new GroupDto(user.Group));
    }
}
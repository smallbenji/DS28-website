using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NETCore.Keycloak.Client.Models.Users;

namespace DS.Aktibasen.Controllers;

[Authorize]
[Route("/api/v1/user")]
public class UsersApiController(IKeycloakActivityHelper keycloakHelper) : Controller
{
    public class UserDTO
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await keycloakHelper.GetUsers();

        var retval = users.Select(u => new UserDTO
        {
            Id = u.Id,
            UserName = u.UserName,
            Email = u.Email,
            Name = ResolveName(u)
        }).ToList();

        return Ok(retval);
    }

    private static string ResolveName(KcUser user)
    {
        var name = string.Join(" ", new[] { user.FirstName, user.LastName }).Trim();
        if (!string.IsNullOrEmpty(name)) return name;

        if (!string.IsNullOrEmpty(user.UserName)) return user.UserName;
        if (!string.IsNullOrEmpty(user.Email)) return user.Email;

        return user.Id;
    }
}

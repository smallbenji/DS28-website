using DS.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers;

public class AccountController(SignInManager<User> signInManager) : Controller
{
    [HttpGet("/logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        return Redirect("/login");
    }
}

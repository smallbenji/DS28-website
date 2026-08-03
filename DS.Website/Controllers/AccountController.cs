using DS.Website.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers;

public class AccountController(UserManager<User> userManager, SignInManager<User> signInManager) : Controller
{
    [HttpGet("/login")]
    public IActionResult Login()
    {
        if (HttpContext.User.Identity.IsAuthenticated)
        {
            return RedirectToAction(nameof(Index));
        }

        return View();
    }

    [HttpGet("/register")]
    public IActionResult Register()
    {
        if (HttpContext.User.Identity.IsAuthenticated)
        {
            return RedirectToAction(nameof(Index));
        }

        return View();
    }

    [HttpPost("/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginInputModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Ugyldigt loginforsøg.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            isPersistent: true,
            lockoutOnFailure: false
        );

        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Denne bruger er blevet låst, venligst kontakt IT");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Ugyldigt loginforsøg.");
        }

        return View(model);
    }

    [HttpPost("/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([FromForm] RegisterInputModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var newUser = new User
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(newUser, model.Password);

        if (result.Succeeded)
        {
            return Redirect("/");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet("/logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        return RedirectToAction(nameof(Login));
    }
}
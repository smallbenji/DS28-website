using System.Security.Claims;
using DS.Backend.Controllers;
using DS.Backend.Extensions;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OptionsFactory = Microsoft.Extensions.Options.Options;

using var users = new RejectingUserManager();
var signIns = new RejectingSignInManager(users);
var account = new AccountApiController(users, signIns)
{
    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
};
var auth = new AuthApiController(users, signIns, null);

foreach (var value in new string[] { null, "", "   " })
{
    RequireBadRequest(await account.EnableTwoFactor(new EnableTwoFactorDto { Code = value }));
    RequireBadRequest(await account.DisableTwoFactor(new DisableTwoFactorDto { Password = value }));
    RequireBadRequest(await auth.TwoFactorLogin(new TwoFactorLoginDto { TwoFactorCode = value }));
}

RequireBadRequest(await account.EnableTwoFactor(null));
RequireBadRequest(await account.DisableTwoFactor(null));
RequireBadRequest(await auth.TwoFactorLogin(null));
RequireBadRequest(await account.EnableTwoFactor(new EnableTwoFactorDto { Code = "invalid" }));
RequireBadRequest(await account.DisableTwoFactor(new DisableTwoFactorDto { Password = "invalid" }));
RequireBadRequest(await auth.TwoFactorLogin(new TwoFactorLoginDto { TwoFactorCode = "invalid" }));

if (users.TokenChecks != 1 || users.PasswordChecks != 1 || signIns.TokenChecks != 1)
{
    throw new InvalidOperationException("Nonempty credentials must be verified before proceeding.");
}

foreach (var environment in new[] { "Development", "Production" })
{
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
    builder.Configuration["ORIGIN"] = "localhost";
    builder.Services.AddLogging();
    builder.Services.AddDSAuth(builder);
    using var services = builder.Services.BuildServiceProvider();
    var cookie = services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
        .Get(IdentityConstants.ApplicationScheme);
    var antiforgery = services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;
    if (cookie.Cookie.SecurePolicy != CookieSecurePolicy.Always ||
        antiforgery.Cookie.SecurePolicy != CookieSecurePolicy.Always)
    {
        throw new InvalidOperationException($"Cookies must require HTTPS in {environment}.");
    }
}

Console.WriteLine("Security alert checks passed: invalid input fails closed, credentials are verified, and cookies require HTTPS.");

static void RequireBadRequest(IActionResult result)
{
    if (result is not BadRequestObjectResult)
    {
        throw new InvalidOperationException("Invalid input must return HTTP 400, never success.");
    }
}

sealed class RejectingUserManager() : UserManager<User>(
    new UserStore<User>(new IdentityDbContext<User>(new DbContextOptions<IdentityDbContext<User>>())),
    OptionsFactory.Create(new IdentityOptions()), new PasswordHasher<User>(), [], [], new UpperInvariantLookupNormalizer(),
    new IdentityErrorDescriber(), new ServiceCollection().BuildServiceProvider(), NullLogger<UserManager<User>>.Instance)
{
    public User TestUser { get; } = new() { Id = "security-check", TwoFactorEnabled = false };
    public int TokenChecks { get; private set; }
    public int PasswordChecks { get; private set; }

    public override Task<User> GetUserAsync(ClaimsPrincipal principal) => Task.FromResult(TestUser);

    public override Task<string> GetAuthenticatorKeyAsync(User user) => Task.FromResult("existing-key");

    public override Task<bool> IsInRoleAsync(User user, string role) => Task.FromResult(false);

    public override Task<bool> VerifyTwoFactorTokenAsync(User user, string tokenProvider, string token)
    {
        TokenChecks++;
        return Task.FromResult(false);
    }

    public override Task<bool> CheckPasswordAsync(User user, string password)
    {
        PasswordChecks++;
        return Task.FromResult(false);
    }

    public override Task<IdentityResult> SetTwoFactorEnabledAsync(User user, bool enabled)
    {
        throw new InvalidOperationException("Invalid credentials must not change 2FA state.");
    }

    public override Task<IdentityResult> ResetAuthenticatorKeyAsync(User user)
    {
        throw new InvalidOperationException("Invalid credentials must not reset the authenticator key.");
    }
}

sealed class RejectingSignInManager(RejectingUserManager users) : SignInManager<User>(
    users, new HttpContextAccessor(), new UserClaimsPrincipalFactory<User>(users, OptionsFactory.Create(new IdentityOptions())),
    OptionsFactory.Create(new IdentityOptions()), NullLogger<SignInManager<User>>.Instance,
    new AuthenticationSchemeProvider(OptionsFactory.Create(new AuthenticationOptions())), new DefaultUserConfirmation<User>())
{
    public int TokenChecks { get; private set; }

    public override Task<User> GetTwoFactorAuthenticationUserAsync() => Task.FromResult(users.TestUser);

    public override Task<Microsoft.AspNetCore.Identity.SignInResult> TwoFactorAuthenticatorSignInAsync(string code, bool isPersistent, bool rememberClient)
    {
        TokenChecks++;
        return Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Failed);
    }
}

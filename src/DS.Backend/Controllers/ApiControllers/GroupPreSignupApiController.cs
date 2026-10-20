using DS.Application.Services.Groups;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [Route("/api/v1/group-pre-signup")]
    public class GroupPreSignupApiController(GroupPreSignupService preSignupService, SignInManager<User> signInManager) : Controller
    {
        [HttpGet("{groupId:int}")]
        public async Task<IActionResult> Lookup(int groupId)
        {
            var result = await preSignupService.Lookup(groupId);
            if (result.Outcome == GroupPreSignupOutcome.NotFound) return NotFound(result.Error);

            if (result.Outcome == GroupPreSignupOutcome.Conflict) return Conflict(result.Error);

            return Ok(result.Details);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GroupPreSignupDto data)
        {
            if (User.Identity?.IsAuthenticated == true) return BadRequest("Log ud, før du opretter en ny bruger til forhåndstilmeldingen.");

            if (data == null || !ModelState.IsValid) return BadRequest("Udfyld navn, gyldig email og adgangskode på mindst 4 tegn. Deltagerantal skal være hele tal på 0 eller derover.");

            var result = await preSignupService.Create(data);
            if (result.Outcome == GroupPreSignupOutcome.NotFound) return NotFound(result.Error);

            if (result.Outcome == GroupPreSignupOutcome.Conflict) return Conflict(result.Error);

            if (result.Outcome == GroupPreSignupOutcome.Invalid) return BadRequest(result.Error);

            await signInManager.SignInAsync(result.NewUser, isPersistent: true);

            return Ok(new { ReturnUrl = "/group" });
        }
    }
}

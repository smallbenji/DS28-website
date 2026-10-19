using DS.Application.Services.Profile;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DS.Website.Controllers
{
    [Route("/api/v1/me")]
    public class MeApiController(MeService service, UserManager<User> userManager) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var result = await service.Index(GetActor());

            return Respond(result);
        }

        [HttpPut("phone")]
        public async Task<IActionResult> UpdatePhone([FromBody] UpdatePhoneDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.UpdatePhone(GetActor(), data);

            return Respond(result);
        }

        [HttpPut("profile-picture")]
        public async Task<IActionResult> UpdateProfilePicture([FromBody] UpdateProfilePictureDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.UpdateProfilePicture(GetActor(), data);

            return Respond(result);
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var result = await service.GetNotifications(GetActor());

            return Respond(result);
        }

        [HttpPut("notifications")]
        public async Task<IActionResult> UpdateNotifications([FromBody] UpdateNotificationPreferencesDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await service.UpdateNotifications(GetActor(), data);

            return Respond(result);
        }

        private MeActor GetActor()
        {
            return new MeActor(userManager.GetUserId(User), HttpContext.User.Identity.IsAuthenticated,
                User.HasClaim(claim => claim.Type == Impersonation.ActiveClaimType), GetEffectiveGroups(User));
        }

        private static List<string> GetEffectiveGroups(ClaimsPrincipal user)
        {
            var roleClaimType = user.Identity is ClaimsIdentity identity
                ? identity.RoleClaimType
                : ClaimTypes.Role;

            return user.FindAll(roleClaimType)
                .Select(claim => claim.Value)
                .Where(value => Enum.TryParse<AppGroups>(value, out _))
                .ToList();
        }

        private IActionResult Respond(MeResult result)
        {
            switch (result.Outcome)
            {
                case MeOutcome.Success:
                    return result.Value == null ? Ok() : Ok(result.Value);
                case MeOutcome.Invalid:
                    return result.Value == null ? BadRequest() : BadRequest(result.Value);
                case MeOutcome.NotFound:
                    return NotFound();
                default:
                    return Forbid();
            }
        }
    }
}

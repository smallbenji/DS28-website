using DS.Application.Services.Users;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Authorize(Roles = nameof(AppRoles.UsersView))]
    [Route("/api/v1/user")]
    public class UserApiController(UserService userService, UserManager<User> userManager) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var result = await userService.GetUsers();

            return Respond(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            if (string.IsNullOrWhiteSpace(data.Email)) return BadRequest("Email er påkrævet.");

            var result = await userService.CreateUser(data);

            return Respond(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UserDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            if (string.IsNullOrWhiteSpace(data.Id)) return BadRequest("Bruger-id er påkrævet.");

            var result = await userService.UpdateUser(data);

            return Respond(result);
        }

        [HttpPut("{id}/profile-picture")]
        [Authorize(Roles = nameof(AppRoles.UsersEditProfilePicture))]
        public async Task<IActionResult> UpdateProfilePicture(string id, [FromBody] UpdateProfilePictureDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await userService.UpdateProfilePicture(userManager.GetUserId(User), id, data);

            return Respond(result);
        }

        [HttpPut("{id}/role/add")]
        public async Task<IActionResult> AddUserToRole([FromBody] string role, string id)
        {
            if (string.IsNullOrWhiteSpace(role)) return BadRequest("Role is required.");

            var result = await userService.AddUserToRole(userManager.GetUserId(User), role, id);

            return Respond(result);
        }

        [HttpPut("{id}/role/remove")]
        public async Task<IActionResult> RemoveUserToRole([FromBody] string role, string id)
        {
            if (string.IsNullOrWhiteSpace(role)) return BadRequest("Role is required.");

            var result = await userService.RemoveUserToRole(userManager.GetUserId(User), role, id);

            return Respond(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(AppRoles.UsersDelete))]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var result = await userService.DeleteUser(id);

            return Respond(result);
        }

        [HttpPut("{id}/lock")]
        [Authorize(Roles = nameof(AppRoles.UsersLock))]
        public async Task<IActionResult> LockUser(string id)
        {
            var result = await userService.LockUser(id);

            return Respond(result);
        }

        [HttpPut("{id}/unlock")]
        [Authorize(Roles = nameof(AppRoles.UsersLock))]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var result = await userService.UnlockUser(id);

            return Respond(result);
        }

        [HttpGet("assignable-groups")]
        public async Task<IActionResult> GetAssignableGroups()
        {
            var result = await userService.GetAssignableGroups(userManager.GetUserId(User));

            return Respond(result);
        }

        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups()
        {
            var result = await userService.GetGroups();

            return Respond(result);
        }

        [HttpPost("{id}/reset-password-link")]
        [Authorize(Roles = nameof(AppRoles.UsersResetPassword))]
        public async Task<IActionResult> CreateResetPasswordLink(string id)
        {
            var result = await userService.CreateResetPasswordLink($"{Request.Scheme}://{Request.Host}", id);

            return Respond(result);
        }

        [HttpPost("invite")]
        public async Task<IActionResult> InviteUser([FromBody] UserInvitationDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var result = await userService.InviteUser(userManager.GetUserId(User), data);

            return Respond(result);
        }

        private IActionResult Respond(UserResult result)
        {
            switch (result.Outcome)
            {
                case UserOutcome.Success:
                    return result.Value == null ? Ok() : Ok(result.Value);
                case UserOutcome.Invalid:
                    return BadRequest(result.Value);
                case UserOutcome.NotFound:
                    return NotFound();
                default:
                    return Forbid();
            }
        }
    }
}

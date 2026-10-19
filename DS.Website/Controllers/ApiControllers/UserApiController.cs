using DS.Domain;
using DS.Domain.Data;
using DS.Domain.DTOs;
using DS.Domain.Models;
using DS.Website.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DS.Website.Controllers
{
    [Authorize(Roles = nameof(AppRoles.UsersView))]
    [Route("/api/v1/user")]
    public class UserApiController(DataDbContext dataDb, UserManager<User> userManager, RoleManager<Role> roleManager, IMemoryCache memoryCache, EmailService emailService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await dataDb.Users
                .OrderBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .Include(x => x.Group)
                .Include(x => x.ProfilePicture)
                .ToListAsync();

            var positionsByUser = await dataDb.OrgPositionHolders
                .AsNoTracking()
                .Where(holder => holder.Position.DeletedAt == null)
                .Select(holder => new
                {
                    holder.UserId,
                    PositionId = holder.Position.Id,
                    PositionTitle = holder.Position.Title
                })
                .ToListAsync();

            var positionsLookup = positionsByUser
                .GroupBy(entry => entry.UserId)
                .ToDictionary(group => group.Key, group => group
                    .Select(entry => new OrgPositionDto
                    {
                        Id = entry.PositionId,
                        Title = entry.PositionTitle
                    })
                    .ToList());

            var roleNamesByUser = await (
                from userRole in dataDb.UserRoles
                join role in dataDb.Roles on userRole.RoleId equals role.Id
                select new { userRole.UserId, role.Name }
            ).AsNoTracking().ToListAsync();

            var rolesLookup = roleNamesByUser
                .GroupBy(entry => entry.UserId)
                .ToDictionary(group => group.Key, group => group.Select(entry => entry.Name).ToList());

            var retval = new List<UserDto>();
            foreach (var user in users)
            {
                var dto = new UserDto(user)
                {
                    Roles = rolesLookup.TryGetValue(user.Id, out var roles) ? roles : [],
                    Positions = positionsLookup.TryGetValue(user.Id, out var positions) ? positions : []
                };

                retval.Add(dto);
            }

            return Ok(retval);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            if (string.IsNullOrWhiteSpace(data.Email)) return BadRequest("Email er påkrævet.");

            var user = new User(data);

            if (data.Group != null)
            {
                var group = await dataDb.Groups.FindAsync(data.Group.Id);
                if (group == null) return BadRequest("Gruppen findes ikke.");

                user.Group = group;
            }

            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok();
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UserDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            if (string.IsNullOrWhiteSpace(data.Id)) return BadRequest("Bruger-id er påkrævet.");

            var user = await userManager.FindByIdAsync(data.Id);
            if (user == null) return NotFound();

            data.ApplyTo(user);

            if (data.Group != null)
            {
                var group = await dataDb.Groups.FindAsync(data.Group.Id);
                if (group == null) return BadRequest("Gruppen findes ikke.");

                user.Group = group;
            }
            else
            {
                dataDb.Entry(user).Property("GroupId").CurrentValue = null;
            }

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok();
        }

        [HttpPut("{id}/profile-picture")]
        [Authorize(Roles = nameof(AppRoles.UsersEditProfilePicture))]
        public async Task<IActionResult> UpdateProfilePicture(string id, [FromBody] UpdateProfilePictureDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var user = await userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (data.Image == null)
            {
                user.ProfilePictureFileId = null;

                var clearResult = await userManager.UpdateAsync(user);
                if (!clearResult.Succeeded) return BadRequest(clearResult.Errors);

                return Ok();
            }

            var currentUserId = userManager.GetUserId(User);

            var image = await dataDb.StoredFiles
                .FirstOrDefaultAsync(f => f.PublicId == data.Image.PublicId
                    && f.Purpose == FilePurpose.ProfilePicture
                    && f.Status == StoredFileStatus.Ready
                    && f.DeletedAt == null
                    && f.UploadedByUserId == currentUserId);
            if (image == null) return BadRequest("Billedet findes ikke.");

            user.ProfilePictureFileId = image.Id;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new ImageReferenceDto(image));
        }

        [HttpPut("{id}/role/add")]
        public async Task<IActionResult> AddUserToRole([FromBody] string role, string id)
        {
            if (string.IsNullOrWhiteSpace(role)) return BadRequest("Role is required.");

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Forbid();

            var currentUserRoles = await userManager.GetRolesAsync(currentUser);
            if (!AppAccess.CanAssignRole(currentUserRoles, role)) return Forbid();

            var user = await userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded) return BadRequest(result.Errors);

            memoryCache.Remove($"{ClaimsTransformer.RoleCachePrefix}{id}");

            return Ok();
        }

        [HttpPut("{id}/role/remove")]
        public async Task<IActionResult> RemoveUserToRole([FromBody] string role, string id)
        {
            if (string.IsNullOrWhiteSpace(role)) return BadRequest("Role is required.");

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Forbid();

            var currentUserRoles = await userManager.GetRolesAsync(currentUser);
            if (!AppAccess.CanAssignRole(currentUserRoles, role)) return Forbid();

            var user = await userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await userManager.RemoveFromRoleAsync(user, role);
            if (!result.Succeeded) return BadRequest(result.Errors);

            memoryCache.Remove($"{ClaimsTransformer.RoleCachePrefix}{id}");

            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(AppRoles.UsersDelete))]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok();
        }

        [HttpPut("{id}/lock")]
        [Authorize(Roles = nameof(AppRoles.UsersLock))]
        public async Task<IActionResult> LockUser(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await userManager.SetLockoutEnabledAsync(user, true);
            var result = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok();
        }

        [HttpPut("{id}/unlock")]
        [Authorize(Roles = nameof(AppRoles.UsersLock))]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok();
        }

        [HttpGet("assignable-groups")]
        public async Task<IActionResult> GetAssignableGroups()
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Forbid();

            var currentUserRoles = await userManager.GetRolesAsync(currentUser);
            var assignable = Enum.GetValues<AppGroups>()
                .Where(g => AppAccess.CanAssignRole(currentUserRoles, g.ToString()))
                .Select(g => g.ToString())
                .ToList();

            return Ok(assignable);
        }

        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups()
        {
            var retval = await roleManager.Roles
                .OrderBy(role => role.Name)
                .Select(role => new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name ?? string.Empty
                })
                .ToListAsync();

            return Ok(retval);
        }

        [HttpPost("{id}/reset-password-link")]
        [Authorize(Roles = nameof(AppRoles.UsersResetPassword))]
        public async Task<IActionResult> CreateResetPasswordLink(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            var link = $"{Request.Scheme}://{Request.Host}/reset-password/{user.Id}?token={Uri.EscapeDataString(token)}";

            return Ok(new ResetPasswordLinkDto
            {
                Link = link,
                Email = user.Email
            });
        }

        [HttpPost("invite")]
        public async Task<IActionResult> InviteUser([FromBody] UserInvitationDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Forbid();

            var currentUserRoles = await userManager.GetRolesAsync(currentUser);
            var allowedRoles = data.Roles
                .Where(r => AppAccess.CanAssignRole(currentUserRoles, r))
                .ToList();
            if (allowedRoles.Count != data.Roles.Count)
            {
                return BadRequest("Du har ikke tilladelse til at tildele nogle af de valgte roller.");
            }

            var newUser = await userManager.FindByEmailAsync(data.Email);
            if (newUser != null) return BadRequest("Bruger findes allerede.");

            var invitation = new UserInvitation()
            {
                InvitationId = Guid.NewGuid(),
                Roles = allowedRoles,
                Email = data.Email
            };

            await dataDb.Invitations.AddAsync(invitation);
            await dataDb.SaveChangesAsync();

            emailService.QueueInvitationMail(invitation);

            return Ok();
        }
    }
}

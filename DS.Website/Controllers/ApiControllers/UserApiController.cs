using DS.DTOs;
using DS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DS.Website.Controllers;

[Authorize(Roles = nameof(AppRoles.UsersView))]
[Route("/api/v1/user")]
public class UserApiController(DataDbContext dataDb, UserManager<User> userManager, RoleManager<Role> roleManager, IMemoryCache memoryCache) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await dataDb.Users
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Include(x => x.Group)
            .ToListAsync();

        var retval = new List<UserDto>();
        foreach (var user in users)
        {
            var dto = new UserDto(user);
            dto.Roles = (await userManager.GetRolesAsync(user)).ToList();
            retval.Add(dto);
        }

        return Ok(retval);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserDto data)
    {
        if (data == null)
        {
            return BadRequest("Invalid request body.");
        }

        if (string.IsNullOrWhiteSpace(data.Email))
        {
            return BadRequest("Email er påkrævet.");
        }

        var user = new User
        {
            UserName = string.IsNullOrWhiteSpace(data.UserName) ? data.Email : data.UserName,
            Email = data.Email,
            FirstName = data.FirstName,
            LastName = data.LastName
        };

        if (data.Group != null)
        {
            var group = await dataDb.Groups.FindAsync(data.Group.Id);
            if (group == null)
            {
                return BadRequest("Gruppen findes ikke.");
            }

            user.Group = group;
        }

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateUser([FromBody] UserDto data)
    {
        if (data == null)
        {
            return BadRequest("Invalid request body.");
        }

        if (string.IsNullOrWhiteSpace(data.Id))
        {
            return BadRequest("Bruger-id er påkrævet.");
        }

        var user = await userManager.FindByIdAsync(data.Id);
        if (user == null)
        {
            return NotFound();
        }

        user.UserName = string.IsNullOrWhiteSpace(data.UserName) ? user.UserName : data.UserName;
        user.Email = data.Email;
        user.FirstName = data.FirstName;
        user.LastName = data.LastName;

        if (data.Group != null)
        {
            var group = await dataDb.Groups.FindAsync(data.Group.Id);
            if (group == null)
            {
                return BadRequest("Gruppen findes ikke.");
            }

            user.Group = group;
        }
        else
        {
            dataDb.Entry(user).Property("GroupId").CurrentValue = null;
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    [HttpPut("{id}/role/add")]
    public async Task<IActionResult> AddUserToRole([FromBody] string role, string id)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return BadRequest("Role is required.");
        }

        var user = await userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var result = await userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        memoryCache.Remove($"{ClaimsTransformer.RoleCachePrefix}{id}");

        return Ok();
    }

    [HttpPut("{id}/role/remove")]
    public async Task<IActionResult> RemoveUserToRole([FromBody] string role, string id)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return BadRequest("Role is required.");
        }

        var user = await userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var result = await userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        memoryCache.Remove($"{ClaimsTransformer.RoleCachePrefix}{id}");

        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(AppRoles.UsersDelete))]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    [HttpPut("{id}/lock")]
    [Authorize(Roles = nameof(AppRoles.UsersLock))]
    public async Task<IActionResult> LockUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        await userManager.SetLockoutEnabledAsync(user, true);
        var result = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    [HttpPut("{id}/unlock")]
    [Authorize(Roles = nameof(AppRoles.UsersLock))]
    public async Task<IActionResult> UnlockUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var result = await userManager.SetLockoutEndDateAsync(user, null);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
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
        if (user == null)
        {
            return NotFound();
        }

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
        if (data == null)
        {
            return BadRequest("Invalid request body.");
        }

        var invitation = new UserInvitation()
        {
            InvitationId = Guid.NewGuid(),
            Roles = data.Roles,
            Email = data.Email
        };

        await dataDb.Invitations.AddAsync(invitation);
        await dataDb.SaveChangesAsync();

//             var message = dSMailer.CreateMessage();
//             message.To.Add(new MailboxAddress("", data.Email));

//             message.Subject = "Velkommen til DS";

//             message.Body = new BodyBuilder
//             {
//                 TextBody = @$"
// Velkommen til DS28!

// Hermed sendes invitations link til oprettelse i DS_OS.

// https://{Request.Host.Value}/invitation/{invitation.InvitationId}
//                 "
//             }.ToMessageBody();

//             await dSMailer.SendMail(message);

        return Ok();
    }
}
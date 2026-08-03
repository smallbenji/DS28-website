using DS.Models;
using DS.Website;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Controllers;

[Authorize(Roles = nameof(AppRoles.UsersView))]
[Route("/api/v1/user")]
public class UserApiController(DataDbContext dataDb, UserManager<User> userManager, RoleManager<Role> roleManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await dataDb.Users
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ToListAsync();

        var retval = new List<UserSummaryDTO>();
        foreach (var user in users)
        {
            retval.Add(new UserSummaryDTO
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                GroupNumber = user.GroupNumber ?? string.Empty,
                Roles = (await userManager.GetRolesAsync(user)).ToList()
            });
        }

        return Ok(retval);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserEditorDTO data)
    {
        if (string.IsNullOrWhiteSpace(data.Email))
        {
            return BadRequest("Email er påkrævet.");
        }

        var user = new User
        {
            UserName = string.IsNullOrWhiteSpace(data.UserName) ? data.Email : data.UserName,
            Email = data.Email,
            FirstName = data.FirstName,
            LastName = data.LastName,
            GroupNumber = data.GroupNumber
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateUser([FromBody] UserEditorDTO data)
    {
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
        user.GroupNumber = data.GroupNumber;

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

        return Ok();
    }

    [HttpPut("{id}/role/remove")]
    public async Task<IActionResult> RemoveUserToRole([FromBody] string role, string id)
    {
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

        return Ok();
    }

    [HttpDelete("{id}")]
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

    [HttpGet("groups")]
    public async Task<IActionResult> GetGroups()
    {
        var retval = await roleManager.Roles
            .OrderBy(role => role.Name)
            .Select(role => new RoleSummaryDTO
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty
            })
            .ToListAsync();

        return Ok(retval);
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteUser([FromBody] InvitationDTO data)
    {
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

public class UserSummaryDTO
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string GroupNumber { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UserEditorDTO
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string GroupNumber { get; set; }
}

public class RoleSummaryDTO
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class InvitationDTO
{
    public List<string> Roles { get; set; }
    public string Email { get; set; }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NETCore.Keycloak.Client.Models.Common;
using NETCore.Keycloak.Client.Models.Users;

namespace DS.HQ.Controllers
{
    [AllowAnonymous]
    [Route("api/v1/invitation")]
    public class InvitationApiController(DataDbContext dataDb, IKeycloakHelper keycloakHelper) : Controller
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvitation(string id)
        {
            Guid.TryParse(id, out var guidId);
            var result = await dataDb.Invitations.FirstOrDefaultAsync(x => x.InvitationId == guidId);

            if (result == null)
            {
                return NotFound("Invitation not found");
            }

            return Ok(result);
        }

        [HttpPost("{id:guid}")]
        public async Task<IActionResult> CreateUser([FromBody] UserInvitationCreationDTO data, Guid id)
        {
            var invitation = await dataDb.Invitations.FirstOrDefaultAsync(x => x.InvitationId == id);

            var user = new DSUser
            {
                User = new KcUser
                {
                    FirstName = data.FirstName,
                    LastName = data.LastName,
                    UserName = data.FirstName.ToLower() + data.LastName.ToLower(),
                    Credentials = new List<KcCredentials>
                    {
                        new()
                        {
                            Type = "password",
                            Value = data.Password,
                            Temporary = false
                        }
                    },
                    Groups = invitation.Roles
                }
            };

            await keycloakHelper.CreateUser(user);

            invitation.Used = true;

            dataDb.Invitations.Update(invitation);
            await dataDb.SaveChangesAsync();

            return Ok();
        }

        public class UserInvitationCreationDTO
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Password { get; set; }
        }
    }
}
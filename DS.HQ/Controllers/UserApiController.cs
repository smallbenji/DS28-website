using DS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimeKit;

namespace DS.HQ.Controllers
{
    [Authorize(Roles = Role.Admin)]
    [Route("/api/v1/user")]
    public class UserApiController : Controller
    {
        private readonly IKeycloakHelper keycloakHelper;
        private readonly DataDbContext dataDb;
        private readonly DSMailer dSMailer;

        public UserApiController(IKeycloakHelper keycloakHelper, DataDbContext dataDb, DSMailer dSMailer)
        {
            this.keycloakHelper = keycloakHelper;
            this.dataDb = dataDb;
            this.dSMailer = dSMailer;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var retval = await keycloakHelper.GetUsers();

            return Ok(retval);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] DSUser data)
        {
            await keycloakHelper.CreateUser(data);

            return Ok();
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] DSUser data)
        {
            await keycloakHelper.UpdateUser(data);

            return Ok();
        }

        [HttpPut("{id}/role/add")]
        public async Task<IActionResult> AddUserToRole([FromBody] string role, string id)
        {
            await keycloakHelper.AddUserToGroup(id, role);

            return Ok();
        }

        [HttpPut("{id}/role/remove")]
        public async Task<IActionResult> RemoveUserToRole([FromBody] string role, string id)
        {
            await keycloakHelper.RemoveUserFromGroup(id, role);

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await keycloakHelper.DeleteUser(id);

            return Ok();
        }

        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups()
        {
            var retval = await keycloakHelper.GetGroups();

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

// {invitation.InvitationId}
//                 "
//             }.ToMessageBody();

//             await dSMailer.SendMail(message);

            return Ok();
        }
    }

    public class InvitationDTO
    {
        public List<string> Roles { get; set; }
        public string Email { get; set; }
    }
}
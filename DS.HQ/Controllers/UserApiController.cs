using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.HQ.Controllers
{
    [Authorize(Roles = Role.Admin)]
    [Route("/api/v1/user")]
    public class UserApiController : Controller
    {
        private readonly KeycloakHelper keycloakHelper;

        public UserApiController(KeycloakHelper keycloakHelper)
        {
            this.keycloakHelper = keycloakHelper;
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
    }
}
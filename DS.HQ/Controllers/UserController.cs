using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NETCore.Keycloak.Client.Models.Groups;
using NETCore.Keycloak.Client.Models.Users;

namespace DS.HQ.Controllers
{
    [Authorize(Roles = Role.Admin)]
    public class UserController : Controller
    {
        private readonly KeycloakHelper keycloakHelper;

        public UserController(KeycloakHelper keycloakHelper)
        {
            this.keycloakHelper = keycloakHelper;
        }

        public async Task<IActionResult> Index()
        {
            var retval = new UserIndexViewModel
            {
                Users = await keycloakHelper.GetUsers(),
            };

            return View(retval);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDetails(string id)
        {
            var retval = new UserDetailsViewModel()
            {
                DSUser = await keycloakHelper.GetUser(id),
                AvailableGroups = await keycloakHelper.GetGroups()
            };

            return PartialView("_UserDetailsPartial", retval);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser([FromForm] UserDetailsViewModel data)
        {
            var currentUser = await keycloakHelper.GetUser(data.DSUser.User.Id);

            currentUser.User.FirstName = data.DSUser.User.FirstName;
            currentUser.User.LastName = data.DSUser.User.LastName;

            if (currentUser.User.Attributes == null)
            {
                currentUser.User.Attributes = new Dictionary<string, object>();
            }

            if (currentUser.GroupNumber == null || !currentUser.GroupNumber.Equals(data.DSUser.GroupNumber))
            {
                currentUser.User.Attributes["groupnumber"] = new List<string> { data.DSUser.GroupNumber };
            }

            var selectedGroups = data.SelectedGroups ?? new List<string>();

            // 1. Add missing groups
            foreach (var groupId in selectedGroups)
            {
                if (!currentUser.Roles.Any(x => x.Id == groupId))
                {
                    await keycloakHelper.AddUserToGroup(currentUser.User.Id, groupId);
                }
            }

            // 2. Remove old groups
            foreach (var existingRole in currentUser.Roles)
            {
                if (!selectedGroups.Contains(existingRole.Id))
                {
                    await keycloakHelper.RemoveUserFromGroup(currentUser.User.Id, existingRole.Id);
                }
            }

            await keycloakHelper.UpdateUser(currentUser);

            return RedirectToAction(nameof(Index));
        }
    }


    public class UserIndexViewModel
    {
        public List<DSUser> Users { get; set; }
    }

    public class UserDetailsViewModel
    {
        public DSUser DSUser { get; set; }
        public List<KcGroup> AvailableGroups { get; set; }
        public List<string> SelectedGroups { get; set; }
    }

    public class DSUser
    {
        public KcUser User { get; set; }
        public string GroupNumber { get; set; }
        public List<KcGroup> Roles { get; set; }
    }
}
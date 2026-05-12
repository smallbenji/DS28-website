using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NETCore.Keycloak.Client.HttpClients.Implementation;
using NETCore.Keycloak.Client.Models.Auth;
using NETCore.Keycloak.Client.Models.Groups;
using NETCore.Keycloak.Client.Models.Users;
using Newtonsoft.Json.Linq;

namespace DS.HQ.Controllers
{
    [Authorize(Roles = "ds-admin")]
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
        public IActionResult GetUserDetails(string id)
        {


            return PartialView("_UserDetailsPartial");
        }
    }


    public class UserIndexViewModel
    {
        public List<DSUser> Users { get; set; }
    }

    public class DSUser
    {
        public KcUser User { get; set; }
        public string GroupNumber { get; set; }
        public List<KcGroup> Roles { get; set; }
    }
}
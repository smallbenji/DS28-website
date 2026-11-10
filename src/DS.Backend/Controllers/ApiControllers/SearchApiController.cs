using DS.Application.Services.Search;
using DS.Backend.Extensions;
using DS.Domain;
using DS.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers.ApiControllers
{
    [Authorize]
    [Route("api/v1/search")]
    public class SearchApiController(SearchService service, UserManager<User> userManager) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            var result = await service.Search(GetActor(), q);

            return this.ToActionResult(result);
        }

        private SearchActor GetActor()
        {
            var isCampAdmin = User.IsInRole(nameof(AppGroups.SysAdmin)) || User.IsInRole(nameof(AppGroups.CampAdmin));

            return new SearchActor(
                userManager.GetUserId(User),
                User.IsInRole(nameof(AppRoles.BoardView)),
                isCampAdmin,
                User.IsInRole(nameof(AppRoles.GroupsView)),
                User.IsInRole(nameof(AppRoles.ActivityView)),
                User.IsInRole(nameof(AppRoles.ActivityAdmin)));
        }
    }
}

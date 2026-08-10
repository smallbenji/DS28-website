using DS.DTOs;
using DS.Models;
using DS.Website.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Authorize(Roles = nameof(AppRoles.ActivityView))]
    [Route("api/v1/activity")]
    public class ActivityApiController(ActivityRepository activityRepository, UserManager<User> userManager) : Controller
    {
        [HttpGet("teams")]
        public async Task<IActionResult> GetTeams()
        {
            var userId = userManager.GetUserId(HttpContext.User);
            var retval = await activityRepository.GetUserActivityTeamAsync(userId);

            return Ok(retval);
        }

        [HttpPost("team/{teamId:int}/activity/add")]
        public async Task<IActionResult> AddActivity([FromBody] ActivityDto data, int teamId)
        {
            var userId = userManager.GetUserId(User);
            if (!await activityRepository.HasAccessToTeam(userId, teamId, true))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            await activityRepository.AddActivity(teamId, new Activity
            {
                Name = data.Name,
            });

            return Ok();
        }

        [HttpPost("teams/add")]
        public async Task<IActionResult> AddTeam([FromBody] ActivityTeamDto activityTeam)
        {
            var userId = userManager.GetUserId(HttpContext.User);
            await activityRepository.AddTeamAsync(activityTeam.Name, userId);

            return Ok();
        }
    }
}
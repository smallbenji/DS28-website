using DS.Application.Services.BackgroundJobs;
using DS.Backend.Extensions;
using DS.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers.ApiControllers
{
    [Authorize(Roles = nameof(AppRoles.HangfireView))]
    [Route("api/v1/background-jobs")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class BackgroundJobApiController(BackgroundJobService backgroundJobService) : Controller
    {
        [HttpGet]
        public IActionResult GetOverview()
        {
            var overview = backgroundJobService.GetOverview(User.IsInRole(nameof(AppRoles.HangfireManage)));

            return Ok(overview);
        }

        [HttpGet("history")]
        public IActionResult GetHistory([FromQuery] string state, [FromQuery] string type, [FromQuery] string search, [FromQuery] int page = 0)
        {
            var history = backgroundJobService.GetHistory(state, type, search, page);

            return Ok(history);
        }

        [HttpGet("{id}")]
        public IActionResult GetDetails(string id)
        {
            var result = backgroundJobService.GetDetails(id);

            return this.ToActionResult(result);
        }

        [Authorize(Roles = nameof(AppRoles.HangfireManage))]
        [HttpPost("{id}/requeue")]
        public IActionResult Requeue(string id)
        {
            var result = backgroundJobService.Requeue(id);

            return this.ToActionResult(result, true);
        }

        [Authorize(Roles = nameof(AppRoles.HangfireManage))]
        [HttpPost("requeue-failed")]
        public IActionResult RequeueFailed()
        {
            var count = backgroundJobService.RequeueFailed();

            return Ok(new { count });
        }

        [Authorize(Roles = nameof(AppRoles.HangfireManage))]
        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            var result = backgroundJobService.Delete(id);

            return this.ToActionResult(result, true);
        }

        [Authorize(Roles = nameof(AppRoles.HangfireManage))]
        [HttpPost("recurring/{id}/trigger")]
        public IActionResult TriggerRecurring(string id)
        {
            var result = backgroundJobService.TriggerRecurring(id);

            return this.ToActionResult(result, true);
        }
    }
}

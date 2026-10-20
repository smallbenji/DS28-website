using DS.Application.Services.Exports;
using DS.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers.ApiControllers
{
    [Authorize(Roles = nameof(AppRoles.ExportsView))]
    [Route("api/v1/exports")]
    public class ExportsApiController(ExportService exportService) : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(exportService.GetExports(GetRoles()));
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> Download(string key, CancellationToken cancellationToken)
        {
            var result = await exportService.Download(key, GetRoles(), cancellationToken);
            if (result.Outcome == ExportOutcome.NotFound) return NotFound("Dataudtrækket findes ikke.");

            if (result.Outcome == ExportOutcome.Forbidden) return Forbid();

            return File(result.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
        }

        private List<string> GetRoles()
        {
            return User.Identities
                .SelectMany(identity => identity.FindAll(identity.RoleClaimType))
                .Select(claim => claim.Value)
                .Distinct()
                .ToList();
        }
    }
}

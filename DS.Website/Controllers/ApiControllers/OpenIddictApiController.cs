using DS.Application.Services.OpenIddict;
using DS.Domain;
using DS.Domain.DTOs;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Authorize(Roles = nameof(AppRoles.OpenIddictManage))]
    [Route("api/v1/openiddict/clients")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class OpenIddictApiController(IOpenIddictClientService clientService, IAntiforgery antiforgery) : Controller
    {
        [HttpGet("roles")]
        public IActionResult GetAvailableRoles()
        {
            return Ok(Enum.GetNames<AppRoles>().OrderBy(role => role).ToArray());
        }

        [HttpGet("antiforgery")]
        public IActionResult GetAntiforgeryToken()
        {
            var tokens = antiforgery.GetAndStoreTokens(HttpContext);
            return Ok(new { token = tokens.RequestToken });
        }

        [HttpGet]
        public async Task<IActionResult> GetClients(CancellationToken cancellationToken)
        {
            var result = await clientService.GetClients(cancellationToken);

            return Respond(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClient([FromBody] OpenIddictClientUpdateDto data, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest("Kontrollér klientens felter.");

            var result = await clientService.CreateClient(data, cancellationToken);

            return Respond(result);
        }

        [HttpPut("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClient(string id, [FromBody] OpenIddictClientUpdateDto data, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest("Kontrollér klientens felter.");

            var result = await clientService.UpdateClient(id, data, cancellationToken);

            return Respond(result);
        }

        [HttpPost("{id}/rotate-secret")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RotateSecret(string id, CancellationToken cancellationToken)
        {
            var result = await clientService.RotateSecret(id, cancellationToken);

            return Respond(result);
        }

        [HttpDelete("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClient(string id, CancellationToken cancellationToken)
        {
            var result = await clientService.DeleteClient(id, cancellationToken);

            return Respond(result, true);
        }

        private IActionResult Respond(OpenIddictClientResult result, bool noContent = false)
        {
            switch (result.Outcome)
            {
                case OpenIddictClientOutcome.Success:
                    if (noContent) return NoContent();

                    return Ok(result.Value);
                case OpenIddictClientOutcome.Invalid:
                    return BadRequest(result.Value);
                case OpenIddictClientOutcome.NotFound:
                    return NotFound();
                default:
                    return Conflict(result.Value);
            }
        }
    }
}

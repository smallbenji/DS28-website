using DS.Application.Services.OpenIddict;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Backend.Extensions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers.ApiControllers
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

            return this.ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] OpenIddictClientUpdateDto data, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest("Kontrollér klientens felter.");

            var result = await clientService.CreateClient(data, cancellationToken);

            return this.ToActionResult(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClient(string id, [FromBody] OpenIddictClientUpdateDto data, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest("Kontrollér klientens felter.");

            var result = await clientService.UpdateClient(id, data, cancellationToken);

            return this.ToActionResult(result);
        }

        [HttpPost("{id}/rotate-secret")]
        public async Task<IActionResult> RotateSecret(string id, CancellationToken cancellationToken)
        {
            var result = await clientService.RotateSecret(id, cancellationToken);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(string id, CancellationToken cancellationToken)
        {
            var result = await clientService.DeleteClient(id, cancellationToken);

            return this.ToActionResult(result, noContent: true);
        }
    }
}

using DS.Application.Services.AccessTokens;
using DS.Backend.Extensions;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers.ApiControllers
{
    [Authorize]
    [Route("api/v1/access-tokens")]
    public class AccessTokenApiController(AccessTokenService service, UserManager<User> userManager) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> GetTokens()
        {
            var result = await service.GetTokens(userManager.GetUserId(User));

            return this.ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateToken([FromBody] AccessTokenCreateDto data)
        {
            var result = await service.CreateToken(userManager.GetUserId(User), data);

            return this.ToActionResult(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> RevokeToken(int id)
        {
            var result = await service.RevokeToken(userManager.GetUserId(User), id);

            return this.ToActionResult(result);
        }
    }
}

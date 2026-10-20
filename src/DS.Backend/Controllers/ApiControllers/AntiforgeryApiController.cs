using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers.ApiControllers
{
    [AllowAnonymous]
    [Route("api/v1/antiforgery")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AntiforgeryApiController(IAntiforgery antiforgery) : Controller
    {
        [HttpGet]
        public IActionResult GetToken()
        {
            var tokens = antiforgery.GetAndStoreTokens(HttpContext);

            return Ok(new { token = tokens.RequestToken });
        }
    }
}

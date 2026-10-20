using DS.Application.Services.Registration;
using DS.Domain;
using DS.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers.ApiControllers
{
    [Route("api/v1/registration-settings")]
    public class RegistrationSettingsApiController(RegistrationSettingsService settingsService) : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await settingsService.GetSettings();
            if (settings == null) return NotFound("Tilmeldingsindstillingerne kunne ikke findes.");

            return Ok(settings);
        }

        [Authorize(Roles = nameof(AppRoles.PreSignupManage))]
        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] RegistrationSettingsDto data)
        {
            if (data == null || !ModelState.IsValid || (data.IsPreSignupOpen == null && data.IsSignupOpen == null))
            {
                return BadRequest("Angiv, om forhåndstilmeldingen eller den endelige tilmelding skal være åben eller lukket.");
            }

            var settings = await settingsService.UpdateSettings(data);
            if (settings == null) return NotFound("Tilmeldingsindstillingerne kunne ikke findes.");

            return Ok(settings);
        }
    }
}

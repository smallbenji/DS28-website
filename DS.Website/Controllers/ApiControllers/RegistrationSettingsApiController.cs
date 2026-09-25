using DS.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Controllers
{
    [Route("api/v1/registration-settings")]
    public class RegistrationSettingsApiController(DataDbContext dataDb) : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await dataDb.RegistrationSettings
                .AsNoTracking()
                .Where(s => s.Id == 1)
                .Select(s => new RegistrationSettingsDto
                {
                    IsPreSignupOpen = s.IsPreSignupOpen,
                    IsSignupOpen = s.IsSignupOpen
                })
                .SingleOrDefaultAsync();
            if (settings == null)
            {
                return NotFound("Tilmeldingsindstillingerne kunne ikke findes.");
            }

            return Ok(settings);
        }

        [Authorize(Roles = nameof(AppRoles.PreSignupManage))]
        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] RegistrationSettingsDto data)
        {
            if (data == null ||
                !ModelState.IsValid ||
                (data.IsPreSignupOpen == null && data.IsSignupOpen == null))
            {
                return BadRequest(
                    "Angiv, om forhåndstilmeldingen eller den endelige tilmelding skal være åben eller lukket."
                );
            }

            var settings = await dataDb.RegistrationSettings
                .SingleOrDefaultAsync(s => s.Id == 1);
            if (settings == null)
            {
                return NotFound("Tilmeldingsindstillingerne kunne ikke findes.");
            }

            if (data.IsPreSignupOpen != null)
            {
                settings.IsPreSignupOpen = data.IsPreSignupOpen.Value;
            }

            if (data.IsSignupOpen != null)
            {
                settings.IsSignupOpen = data.IsSignupOpen.Value;
            }

            await dataDb.SaveChangesAsync();

            return Ok(new RegistrationSettingsDto
            {
                IsPreSignupOpen = settings.IsPreSignupOpen,
                IsSignupOpen = settings.IsSignupOpen
            });
        }
    }
}

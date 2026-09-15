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
                .Select(s => new RegistrationSettingsDto { IsPreSignupOpen = s.IsPreSignupOpen })
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
            if (data == null || !ModelState.IsValid)
            {
                return BadRequest("Angiv, om forhåndstilmeldingen skal være åben eller lukket.");
            }

            var updatedCount = await dataDb.RegistrationSettings
                .Where(s => s.Id == 1)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPreSignupOpen, data.IsPreSignupOpen));
            if (updatedCount == 0)
            {
                return NotFound("Tilmeldingsindstillingerne kunne ikke findes.");
            }

            return Ok(data);
        }
    }
}

using DS.Application.Services.Registration;
using DS.Backend.Extensions;
using DS.Domain;
using DS.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers.ApiControllers
{
    [Authorize]
    [Route("api/v1/camp-days")]
    public class CampDayApiController(CampDayService campDayService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> GetDays()
        {
            var days = await campDayService.GetDays();

            return Ok(days);
        }

        [Authorize(Roles = nameof(AppRoles.PreSignupManage))]
        [HttpPost]
        public async Task<IActionResult> CreateDay([FromBody] CampDayUpsertDto data)
        {
            var result = await campDayService.CreateDay(data);

            return this.ToActionResult(result);
        }

        [Authorize(Roles = nameof(AppRoles.PreSignupManage))]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateDay(int id, [FromBody] CampDayUpsertDto data)
        {
            var result = await campDayService.UpdateDay(id, data);

            return this.ToActionResult(result);
        }

        [Authorize(Roles = nameof(AppRoles.PreSignupManage))]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteDay(int id)
        {
            var result = await campDayService.DeleteDay(id);

            return this.ToActionResult(result, noContent: true);
        }
    }
}

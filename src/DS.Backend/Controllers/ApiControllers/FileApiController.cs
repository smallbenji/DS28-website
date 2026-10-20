using DS.Application.Services.Files;
using DS.Domain;
using DS.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DS.Backend.Controllers.ApiControllers
{
    [Authorize]
    [Route("api/v1/files")]
    public class FileApiController(
        FileService fileService,
        IOptions<DSSettings> options,
        UserManager<User> userManager) : Controller
    {
        [HttpPost("{purpose}")]
        public async Task<IActionResult> Upload(string purpose, [FromForm] IFormFile file)
        {
            if (!TryParsePurpose(purpose, out var filePurpose)) return BadRequest("Ukendt filtype.");
            if (file == null || file.Length == 0) return BadRequest("Ingen fil blev uploadet.");
            if (file.Length > options.Value.MaxUploadBytes) return BadRequest("Filen er for stor.");
            if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Kun billedfiler understøttes.");
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            using var stream = file.OpenReadStream();
            var storedFile = await fileService.Upload(user.Id, file.FileName, file.ContentType, filePurpose, stream);

            return Ok(storedFile);
        }

        [AllowAnonymous]
        [HttpGet("public/{publicId:guid}")]
        public async Task<IActionResult> GetPublic(Guid publicId)
        {
            var file = await fileService.Get(publicId, true);
            if (file == null) return NotFound();

            return Serve(file);
        }

        [HttpGet("{publicId:guid}")]
        public async Task<IActionResult> Get(Guid publicId)
        {
            var file = await fileService.Get(publicId);
            if (file == null) return NotFound();

            return Serve(file);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await fileService.Delete(id, userManager.GetUserId(User), User.IsInRole(nameof(AppGroups.SysAdmin)));
            if (result == FileDeleteResult.NotFound) return NotFound();

            if (result == FileDeleteResult.Forbidden) return Forbid();

            return Ok();
        }

        private IActionResult Serve(FileDownload file)
        {
            Response.Headers["Cache-Control"] = file.IsPublic
                ? "public, max-age=31536000, immutable"
                : "private, max-age=3600";

            return File(file.Content, file.ContentType, enableRangeProcessing: true);
        }

        private static bool TryParsePurpose(string value, out FilePurpose purpose)
        {
            switch (value?.ToLowerInvariant())
            {
                case "catalog":
                    purpose = FilePurpose.CatalogImage;
                    return true;
                case "profile":
                    purpose = FilePurpose.ProfilePicture;
                    return true;
                default:
                    purpose = default;
                    return false;
            }
        }
    }
}

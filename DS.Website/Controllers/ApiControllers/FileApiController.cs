using DS;
using DS.Data;
using DS.DTOs;
using DS.Models;
using DS.Website.Services;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DS.Website.Controllers
{
    [Authorize]
    [Route("api/v1/files")]
    public class FileApiController(
        DataDbContext dataDb,
        IFileStorage storage,
        IBackgroundJobClient backgroundJobs,
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

            var storedFile = new StoredFile
            {
                PublicId = Guid.NewGuid(),
                OriginalName = file.FileName,
                ContentType = file.ContentType,
                Purpose = filePurpose,
                IsPublic = filePurpose == FilePurpose.CatalogImage,
                Status = StoredFileStatus.Pending,
                UploadedByUserId = user.Id,
            };

            dataDb.StoredFiles.Add(storedFile);
            await dataDb.SaveChangesAsync();

            using (var stream = file.OpenReadStream())
            {
                await storage.WriteAsync(FileKeys.Pending(storedFile.Id), stream);
            }

            backgroundJobs.Enqueue<FileProcessingJobs>(jobs => jobs.ConvertToWebp(storedFile.Id));

            return Ok(new StoredFileDto(storedFile));
        }

        [AllowAnonymous]
        [HttpGet("public/{publicId:guid}")]
        public async Task<IActionResult> GetPublic(Guid publicId)
        {
            var file = await dataDb.StoredFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.PublicId == publicId && f.IsPublic && f.Status == StoredFileStatus.Ready && f.DeletedAt == null);
            if (file == null) return NotFound();

            return await ServeAsync(file);
        }

        [HttpGet("{publicId:guid}")]
        public async Task<IActionResult> Get(Guid publicId)
        {
            var file = await dataDb.StoredFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.PublicId == publicId && f.Status == StoredFileStatus.Ready && f.DeletedAt == null);
            if (file == null) return NotFound();

            return await ServeAsync(file);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var file = await dataDb.StoredFiles.FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null);
            if (file == null) return NotFound();

            var userId = userManager.GetUserId(User);
            if (file.UploadedByUserId != userId && !User.IsInRole(nameof(AppGroups.SysAdmin))) return Forbid();

            file.DeletedAt = DateTime.UtcNow;
            file.UpdatedAt = DateTime.UtcNow;
            await dataDb.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(file.StorageKey)) await storage.DeleteAsync(file.StorageKey);
            await storage.DeleteAsync(FileKeys.Pending(file.Id));

            return Ok();
        }

        private async Task<IActionResult> ServeAsync(StoredFile file)
        {
            if (string.IsNullOrWhiteSpace(file.StorageKey)) return NotFound();
            if (!await storage.ExistsAsync(file.StorageKey)) return NotFound();

            var stream = await storage.OpenReadAsync(file.StorageKey);
            Response.Headers["Cache-Control"] = file.IsPublic
                ? "public, max-age=31536000, immutable"
                : "private, max-age=3600";

            return File(stream, file.ContentType ?? "application/octet-stream", enableRangeProcessing: true);
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

using DS.Data;
using DS.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Services
{
    public class FileProcessingJobs(DataDbContext dataDb, IFileStorage storage, ILogger<FileProcessingJobs> logger)
    {
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 120, 300 })]
        public async Task ConvertToWebp(int fileId)
        {
            var file = await dataDb.StoredFiles.FirstOrDefaultAsync(f => f.Id == fileId);
            if (file == null)
            {
                logger.LogWarning("StoredFile {FileId} findes ikke; springer konvertering over.", fileId);
                return;
            }

            if (file.Status == StoredFileStatus.Ready) return;

            file.Status = StoredFileStatus.Processing;
            file.UpdatedAt = DateTime.UtcNow;
            await dataDb.SaveChangesAsync();

            var pendingKey = FileKeys.Pending(file.Id);

            try
            {
                if (!await storage.ExistsAsync(pendingKey))
                {
                    throw new FileNotFoundException($"Den midlertidige fil for {fileId} findes ikke.");
                }

                using var source = await storage.OpenReadAsync(pendingKey);
                var square = file.Purpose == FilePurpose.ProfilePicture;
                var result = await WebpImageProcessor.ConvertAsync(source, square);

                var finalKey = FileKeys.Image(file.PublicId);
                using (var content = new MemoryStream(result.Content))
                {
                    await storage.WriteAsync(finalKey, content);
                }

                file.StorageKey = finalKey;
                file.ContentType = "image/webp";
                file.SizeBytes = result.SizeBytes;
                file.Sha256 = result.Sha256;
                file.Width = result.Width;
                file.Height = result.Height;
                file.Status = StoredFileStatus.Ready;
                file.Error = null;
                file.UpdatedAt = DateTime.UtcNow;
                await dataDb.SaveChangesAsync();

                await storage.DeleteAsync(pendingKey);

                logger.LogInformation("Konverterede fil {FileId} til WebP.", fileId);
            }
            catch (Exception ex)
            {
                file.Status = StoredFileStatus.Failed;
                file.Error = ex.Message;
                file.UpdatedAt = DateTime.UtcNow;
                await dataDb.SaveChangesAsync();

                logger.LogWarning(ex, "Kunne ikke konvertere fil {FileId} til WebP.", fileId);
                throw;
            }
        }
    }
}

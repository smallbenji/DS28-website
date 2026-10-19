using DS.Application.Data;
using DS.Application.FileStorage;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Files
{
    public class FileService(IDataDbContext dataDb, IFileStorage storage, IFileProcessingQueue processingQueue)
    {
        public async Task<StoredFileDto> Upload(string userId, string originalName, string contentType, FilePurpose purpose, Stream content)
        {
            var storedFile = new StoredFile
            {
                PublicId = Guid.NewGuid(),
                OriginalName = originalName,
                ContentType = contentType,
                Purpose = purpose,
                IsPublic = purpose == FilePurpose.CatalogImage,
                Status = StoredFileStatus.Pending,
                UploadedByUserId = userId,
            };

            dataDb.StoredFiles.Add(storedFile);
            await dataDb.SaveChangesAsync();

            await storage.WriteAsync(FileKeys.Pending(storedFile.Id), content);
            processingQueue.Enqueue(storedFile.Id);

            return new StoredFileDto(storedFile);
        }

        public async Task<FileDownload> Get(Guid publicId, bool publicOnly = false)
        {
            var file = await dataDb.StoredFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.PublicId == publicId
                    && (!publicOnly || f.IsPublic)
                    && f.Status == StoredFileStatus.Ready
                    && f.DeletedAt == null);
            if (file == null || string.IsNullOrWhiteSpace(file.StorageKey)) return null;

            if (!await storage.ExistsAsync(file.StorageKey)) return null;

            var content = await storage.OpenReadAsync(file.StorageKey);

            return new FileDownload(content, file.ContentType ?? "application/octet-stream", file.IsPublic);
        }

        public async Task<FileDeleteResult> Delete(int id, string userId, bool isSystemAdministrator)
        {
            var file = await dataDb.StoredFiles.FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null);
            if (file == null) return FileDeleteResult.NotFound;

            if (file.UploadedByUserId != userId && !isSystemAdministrator) return FileDeleteResult.Forbidden;

            file.DeletedAt = DateTime.UtcNow;
            file.UpdatedAt = DateTime.UtcNow;
            await dataDb.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(file.StorageKey)) await storage.DeleteAsync(file.StorageKey);
            await storage.DeleteAsync(FileKeys.Pending(file.Id));

            return FileDeleteResult.Success;
        }
    }
}

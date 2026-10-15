using System.Net;
using DS;
using DS.Website.Repositories;
using Google;
using Google.Apis.Drive.v3;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DS.Website.Services
{
    public class GoogleDriveService(
        IOptions<DSSettings> options,
        IGoogleAuthService googleAuth,
        ActivityRepository activityRepository,
        IMemoryCache cache) : IGoogleDriveService
    {
        private static readonly TimeSpan FilesCacheDuration = TimeSpan.FromSeconds(30);

        public async Task<List<DriveFileInfo>> GetActivityFilesAsync(int activityId, CancellationToken cancellationToken = default)
        {
            var cacheKey = FilesCacheKey(activityId);

            if (cache.TryGetValue(cacheKey, out List<DriveFileInfo> cached) && cached != null)
            {
                return cached;
            }

            var service = await googleAuth.GetDriveServiceAsync(cancellationToken);
            var folderId = await ResolveActivityFolderAsync(service, activityId, cancellationToken);

            if (folderId == null)
            {
                cache.Set(cacheKey, new List<DriveFileInfo>(), FilesCacheDuration);

                return [];
            }

            List<DriveFileInfo> files;
            try
            {
                files = await ListFilesAsync(service, folderId, cancellationToken);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                await activityRepository.RemoveDriveFolderIdAsync(activityId);
                files = [];
            }

            cache.Set(cacheKey, files, FilesCacheDuration);

            return files;
        }

        public async Task<string> CreateActivityFileAsync(int activityId, string activityName, string kind, string title, CancellationToken cancellationToken = default)
        {
            var service = await googleAuth.GetDriveServiceAsync(cancellationToken);
            var folderId = await EnsureActivityFolderAsync(service, activityId, activityName, cancellationToken);

            var file = new Google.Apis.Drive.v3.Data.File
            {
                Name = title,
                MimeType = GoogleFileTypes.MimeTypeForKind(kind),
                Parents = [folderId],
            };

            var request = service.Files.Create(file);
            request.SupportsAllDrives = true;
            request.Fields = "id";

            var created = await request.ExecuteAsync(cancellationToken);

            cache.Remove(FilesCacheKey(activityId));

            return created.Id;
        }

        private async Task<string> ResolveActivityFolderAsync(DriveService service, int activityId, CancellationToken cancellationToken)
        {
            var folderId = await activityRepository.GetDriveFolderIdAsync(activityId);

            if (folderId != null) return folderId;

            folderId = await FindActivityFolderAsync(service, activityId, cancellationToken);

            if (folderId == null) return null;

            await activityRepository.SaveDriveFolderIdAsync(activityId, folderId);

            return folderId;
        }

        private async Task<string> EnsureActivityFolderAsync(DriveService service, int activityId, string activityName, CancellationToken cancellationToken)
        {
            var folderId = await activityRepository.GetDriveFolderIdAsync(activityId);

            if (folderId != null && await FolderExistsAsync(service, folderId, cancellationToken))
            {
                return folderId;
            }

            if (folderId != null)
            {
                await activityRepository.RemoveDriveFolderIdAsync(activityId);
            }

            folderId = await FindActivityFolderAsync(service, activityId, cancellationToken);

            if (folderId == null)
            {
                folderId = await CreateActivityFolderAsync(service, activityId, activityName, cancellationToken);
            }

            await activityRepository.SaveDriveFolderIdAsync(activityId, folderId);

            return folderId;
        }

        private async Task<List<DriveFileInfo>> ListFilesAsync(DriveService service, string folderId, CancellationToken cancellationToken)
        {
            var files = new List<DriveFileInfo>();
            string pageToken = null;

            do
            {
                var request = service.Files.List();
                request.Q = $"'{folderId}' in parents and mimeType != '{GoogleFileTypes.FolderMimeType}' and trashed = false";
                request.OrderBy = "modifiedTime desc";
                request.PageSize = 200;
                request.PageToken = pageToken;
                request.Fields = "nextPageToken, files(id, name, mimeType, modifiedTime)";
                request.SupportsAllDrives = true;
                request.IncludeItemsFromAllDrives = true;

                var listed = await request.ExecuteAsync(cancellationToken);

                if (listed.Files != null)
                {
                    files.AddRange(listed.Files.Select(ToDriveFileInfo));
                }

                pageToken = listed.NextPageToken;
            }
            while (!string.IsNullOrEmpty(pageToken));

            return files;
        }

        private static async Task<bool> FolderExistsAsync(DriveService service, string folderId, CancellationToken cancellationToken)
        {
            try
            {
                var request = service.Files.Get(folderId);
                request.Fields = "id, trashed";
                request.SupportsAllDrives = true;

                var folder = await request.ExecuteAsync(cancellationToken);

                return folder != null && folder.Trashed != true;
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        private async Task<string> FindActivityFolderAsync(DriveService service, int activityId, CancellationToken cancellationToken)
        {
            var request = service.Files.List();
            request.Q = $"mimeType = '{GoogleFileTypes.FolderMimeType}' and appProperties has {{ key='activity_id' and value='{activityId}' }} and trashed = false";
            request.OrderBy = "createdTime";
            request.PageSize = 10;
            request.Fields = "files(id)";
            request.SupportsAllDrives = true;
            request.IncludeItemsFromAllDrives = true;

            var listed = await request.ExecuteAsync(cancellationToken);

            return listed.Files?.FirstOrDefault()?.Id;
        }

        private async Task<string> CreateActivityFolderAsync(DriveService service, int activityId, string activityName, CancellationToken cancellationToken)
        {
            var folder = new Google.Apis.Drive.v3.Data.File
            {
                Name = string.IsNullOrWhiteSpace(activityName) ? $"Aktivitet {activityId}" : activityName,
                MimeType = GoogleFileTypes.FolderMimeType,
                Parents = [options.Value.GoogleDriveBaseFolderId],
                AppProperties = new Dictionary<string, string>
                {
                    { "activity_id", activityId.ToString() },
                },
            };

            var request = service.Files.Create(folder);
            request.SupportsAllDrives = true;
            request.Fields = "id";

            var created = await request.ExecuteAsync(cancellationToken);

            return created.Id;
        }

        private static DriveFileInfo ToDriveFileInfo(Google.Apis.Drive.v3.Data.File file)
        {
            return new DriveFileInfo
            {
                Id = file.Id,
                Name = file.Name,
                MimeType = file.MimeType,
                ModifiedAt = file.ModifiedTimeDateTimeOffset?.UtcDateTime,
            };
        }

        private static string FilesCacheKey(int activityId)
        {
            return $"activity-files:{activityId}";
        }
    }
}

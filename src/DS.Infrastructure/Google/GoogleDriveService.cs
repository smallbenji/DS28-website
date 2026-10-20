using DS.Application.Services.Google;
using DriveFile = global::Google.Apis.Drive.v3.Data.File;
using System.Net;
using DS.Application.Services.Activities;
using DS.Domain;
using Google;
using Google.Apis.Drive.v3;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DS.Infrastructure.Google
{
    public class GoogleDriveService(
        IOptions<DSSettings> options,
        IGoogleDriveClientFactory driveClientFactory,
        ActivityService activityService,
        IMemoryCache cache) : IGoogleDriveService
    {
        private const string ActivityRootFolderKey = "root";
        private const int MaxFolderDepth = 20;

        private static readonly TimeSpan FilesCacheDuration = TimeSpan.FromSeconds(30);

        public async Task<List<DriveFileInfo>> GetActivityFilesAsync(int activityId, string folderId, CancellationToken cancellationToken = default)
        {
            var folderKey = string.IsNullOrWhiteSpace(folderId) ? ActivityRootFolderKey : folderId;
            var cacheKey = FilesCacheKey(activityId, folderKey);

            if (cache.TryGetValue(cacheKey, out List<DriveFileInfo> cached) && cached != null)
            {
                return cached;
            }

            var service = await driveClientFactory.GetDriveServiceAsync(cancellationToken);
            var rootId = await ResolveActivityFolderAsync(service, activityId, cancellationToken);
            var targetId = rootId;

            if (!string.IsNullOrWhiteSpace(folderId))
            {
                if (rootId == null || !await IsInsideFolderAsync(service, rootId, folderId, cancellationToken)) return null;

                targetId = folderId;
            }

            if (targetId == null)
            {
                cache.Set(cacheKey, new List<DriveFileInfo>(), FilesCacheDuration);

                return [];
            }

            List<DriveFileInfo> files;
            try
            {
                files = await ListFilesAsync(service, targetId, cancellationToken);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                if (targetId == rootId)
                {
                    await activityService.RemoveDriveFolderIdAsync(activityId);
                }

                files = [];
            }

            cache.Set(cacheKey, files, FilesCacheDuration);

            return files;
        }

        public async Task<string> CreateActivityFileAsync(int activityId, string activityName, string kind, string title, string folderId, CancellationToken cancellationToken = default)
        {
            var service = await driveClientFactory.GetDriveServiceAsync(cancellationToken);
            var rootId = await EnsureActivityFolderAsync(service, activityId, activityName, cancellationToken);

            var targetId = rootId;
            var folderKey = ActivityRootFolderKey;

            if (!string.IsNullOrWhiteSpace(folderId))
            {
                if (!await IsInsideFolderAsync(service, rootId, folderId, cancellationToken)) return null;

                targetId = folderId;
                folderKey = folderId;
            }

            var file = new DriveFile
            {
                Name = title,
                MimeType = GoogleFileTypes.MimeTypeForKind(kind),
                Parents = [targetId],
            };

            var request = service.Files.Create(file);
            request.SupportsAllDrives = true;
            request.Fields = "id";

            var created = await request.ExecuteAsync(cancellationToken);

            cache.Remove(FilesCacheKey(activityId, folderKey));

            return created.Id;
        }

        private static async Task<bool> IsInsideFolderAsync(DriveService service, string rootId, string folderId, CancellationToken cancellationToken)
        {
            var current = folderId;

            for (var depth = 0; depth <= MaxFolderDepth; depth++)
            {
                if (current == rootId) return true;

                DriveFile folder;
                try
                {
                    var request = service.Files.Get(current);
                    request.Fields = "id, mimeType, parents, trashed";
                    request.SupportsAllDrives = true;

                    folder = await request.ExecuteAsync(cancellationToken);
                }
                catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                if (folder == null || folder.MimeType != GoogleFileTypes.FolderMimeType || folder.Trashed == true) return false;

                current = folder.Parents?.FirstOrDefault();

                if (string.IsNullOrEmpty(current)) return false;
            }

            return false;
        }

        private async Task<string> ResolveActivityFolderAsync(DriveService service, int activityId, CancellationToken cancellationToken)
        {
            var folderId = await activityService.GetDriveFolderIdAsync(activityId);

            if (folderId != null) return folderId;

            folderId = await FindActivityFolderAsync(service, activityId, cancellationToken);

            if (folderId == null) return null;

            await activityService.SaveDriveFolderIdAsync(activityId, folderId);

            return folderId;
        }

        private async Task<string> EnsureActivityFolderAsync(DriveService service, int activityId, string activityName, CancellationToken cancellationToken)
        {
            var folderId = await activityService.GetDriveFolderIdAsync(activityId);

            if (folderId != null && await FolderExistsAsync(service, folderId, cancellationToken))
            {
                return folderId;
            }

            if (folderId != null)
            {
                await activityService.RemoveDriveFolderIdAsync(activityId);
            }

            folderId = await FindActivityFolderAsync(service, activityId, cancellationToken);

            if (folderId == null)
            {
                folderId = await CreateActivityFolderAsync(service, activityId, activityName, cancellationToken);
            }

            await activityService.SaveDriveFolderIdAsync(activityId, folderId);

            return folderId;
        }

        private static async Task<List<DriveFileInfo>> ListFilesAsync(DriveService service, string folderId, CancellationToken cancellationToken)
        {
            var files = new List<DriveFileInfo>();
            string pageToken = null;

            do
            {
                var request = service.Files.List();
                request.Q = $"'{folderId}' in parents and trashed = false";
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

            var folders = files.Where(IsFolder);
            var others = files.Where(file => !IsFolder(file));

            return folders.Concat(others).ToList();
        }

        private static bool IsFolder(DriveFileInfo file)
        {
            return file.MimeType == GoogleFileTypes.FolderMimeType;
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
            var folder = new DriveFile
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

        private static DriveFileInfo ToDriveFileInfo(DriveFile file)
        {
            return new DriveFileInfo
            {
                Id = file.Id,
                Name = file.Name,
                MimeType = file.MimeType,
                ModifiedAt = file.ModifiedTimeDateTimeOffset?.UtcDateTime,
            };
        }

        private static string FilesCacheKey(int activityId, string folderKey)
        {
            return $"activity-files:{activityId}:{folderKey}";
        }
    }
}

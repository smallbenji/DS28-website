using DS.Application.Data;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Activities;

public class ActivityService(IDataDbContext dataDb)
{
    public async Task<List<Activity>> GetUserActivities(string userId)
    {
        return await dataDb.ActivityTeamMemberships
            .AsNoTracking()
            .Where(m => m.User.Id == userId)
            .SelectMany(m => m.ActivityTeam.Activities)
            .Include(a => a.ReviewedBy)
            .Include(a => a.Budget)
            .Include(a => a.Catalog)
                .ThenInclude(c => c.ImageFile)
            .ToListAsync();
    }

    public async Task AddActivity(int teamId, Activity activity)
    {
        activity.ActivityTeamId = teamId;

        await dataDb.Activities.AddAsync(activity);

        await dataDb.SaveChangesAsync();
    }

    public async Task<Activity> GetActivity(int activityId)
    {
        return await dataDb.Activities
            .AsNoTracking()
            .Include(a => a.ReviewedBy)
            .Include(a => a.Budget)
            .Include(a => a.Catalog)
                .ThenInclude(c => c.ImageFile)
            .FirstOrDefaultAsync(a => a.Id == activityId && a.DeletedAt == null);
    }

    public async Task<ActivityChangeResult> UpdateActivityAsync(int activityId, ActivityUpdateDto data, string userId)
    {
        var activity = await dataDb.Activities
            .Include(a => a.Catalog)
            .FirstOrDefaultAsync(a => a.Id == activityId && a.DeletedAt == null);
        if (activity == null) return ActivityChangeResult.NotFound;
        if (activity.ReviewVersion != data.ReviewVersion) return ActivityChangeResult.Conflict;

        var contentChanged = activity.Name != data.Name
            || (data.Catalog != null && (activity.Catalog?.Name != data.Catalog.Name
                || activity.Catalog?.Summary != data.Catalog.Summary
                || activity.Catalog?.Description != data.Catalog.Description));

        int? newImageId = null;
        if (data.Catalog?.Image != null)
        {
            var image = await dataDb.StoredFiles
                .FirstOrDefaultAsync(f => f.PublicId == data.Catalog.Image.PublicId
                    && f.Purpose == FilePurpose.CatalogImage
                    && f.Status == StoredFileStatus.Ready
                    && f.DeletedAt == null);
            if (image == null || (image.Id != activity.Catalog?.ImageFileId && image.UploadedByUserId != userId)) return ActivityChangeResult.InvalidImage;

            newImageId = image.Id;
            contentChanged |= activity.Catalog?.ImageFileId != newImageId;
        }

        if (!contentChanged) return ActivityChangeResult.Success;

        activity.Name = data.Name;
        if (data.Catalog != null)
        {
            activity.Catalog ??= new CatalogData();
            activity.Catalog.Name = data.Catalog.Name;
            activity.Catalog.Summary = data.Catalog.Summary;
            activity.Catalog.Description = data.Catalog.Description;
            if (newImageId.HasValue) activity.Catalog.ImageFileId = newImageId;
        }

        ActivityReview.ContentChanged(activity);
        return await SaveActivityChangeAsync();
    }

    public async Task<ActivityChangeResult> ReviewActivity(int activityId, int version, ActivityReviewAction action, string userId, string feedback = null)
    {
        var activity = await dataDb.Activities
            .Include(a => a.Catalog)
            .FirstOrDefaultAsync(a => a.Id == activityId && a.DeletedAt == null);
        if (activity == null) return ActivityChangeResult.NotFound;
        if (activity.ReviewVersion != version) return ActivityChangeResult.Conflict;

        var result = ActivityReview.Apply(activity, action, userId, feedback, DateTime.UtcNow);
        if (result != ActivityChangeResult.Success) return result;

        return await SaveActivityChangeAsync();
    }

    public async Task<List<ActivityFile>> GetFilesAsync(int activityId)
    {
        return await dataDb.ActivityFiles
            .AsNoTracking()
            .Include(f => f.CreatedBy)
            .Where(f => f.ActivityId == activityId && f.DeletedAt == null)
            .ToListAsync();
    }

    public async Task<ActivityFile> AddFileAsync(int activityId, string driveFileId, string title, string userId)
    {
        var file = new ActivityFile
        {
            ActivityId = activityId,
            DriveFileId = driveFileId,
            Title = title,
            CreatedByUserId = userId,
        };

        dataDb.ActivityFiles.Add(file);
        await dataDb.SaveChangesAsync();

        return file;
    }

    public async Task<string> GetDriveFolderIdAsync(int activityId)
    {
        return await dataDb.ActivityDriveFolders
            .AsNoTracking()
            .Where(f => f.ActivityId == activityId)
            .Select(f => f.DriveFolderId)
            .FirstOrDefaultAsync();
    }

    public async Task SaveDriveFolderIdAsync(int activityId, string driveFolderId)
    {
        var folder = await dataDb.ActivityDriveFolders.FirstOrDefaultAsync(f => f.ActivityId == activityId);

        if (folder == null)
        {
            dataDb.ActivityDriveFolders.Add(new ActivityDriveFolder
            {
                ActivityId = activityId,
                DriveFolderId = driveFolderId,
            });
        }
        else
        {
            folder.DriveFolderId = driveFolderId;
            folder.UpdatedAt = DateTime.UtcNow;
        }

        await dataDb.SaveChangesAsync();
    }

    public async Task RemoveDriveFolderIdAsync(int activityId)
    {
        var folder = await dataDb.ActivityDriveFolders.FirstOrDefaultAsync(f => f.ActivityId == activityId);

        if (folder == null) return;

        dataDb.ActivityDriveFolders.Remove(folder);
        await dataDb.SaveChangesAsync();
    }

    private async Task<ActivityChangeResult> SaveActivityChangeAsync()
    {
        try
        {
            await dataDb.SaveChangesAsync();
            return ActivityChangeResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return ActivityChangeResult.Conflict;
        }
    }
}
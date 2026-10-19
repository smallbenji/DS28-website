namespace DS.Domain.Models;

public enum ActivityReviewStatus
{
    Draft,
    Submitted,
    ChangesRequested,
    Approved
}

public enum ActivityReviewAction
{
    Submit,
    Approve,
    RequestChanges
}

public enum ActivityChangeResult
{
    Success,
    NotFound,
    Conflict,
    InvalidState,
    Incomplete,
    InvalidImage,
    InvalidFeedback
}

public static class ActivityReview
{
    public static List<string> MissingFields(Activity activity)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(activity.Name)) missing.Add("Navn");
        if (string.IsNullOrWhiteSpace(activity.Catalog?.Name)) missing.Add("Katalognavn");
        if (string.IsNullOrWhiteSpace(activity.Catalog?.Summary)) missing.Add("Resumé");
        if (string.IsNullOrWhiteSpace(activity.Catalog?.Description)) missing.Add("Beskrivelse");
        return missing;
    }

    public static ActivityChangeResult Apply(Activity activity, ActivityReviewAction action, string userId, string feedback, DateTime now)
    {
        if (action == ActivityReviewAction.Submit)
        {
            if (activity.ReviewStatus != ActivityReviewStatus.Draft && activity.ReviewStatus != ActivityReviewStatus.ChangesRequested) return ActivityChangeResult.InvalidState;
            if (MissingFields(activity).Count > 0) return ActivityChangeResult.Incomplete;

            activity.ReviewStatus = ActivityReviewStatus.Submitted;
            activity.SubmittedAt = now;
        }
        else
        {
            if (activity.ReviewStatus != ActivityReviewStatus.Submitted) return ActivityChangeResult.InvalidState;
            if (action == ActivityReviewAction.RequestChanges && (string.IsNullOrWhiteSpace(feedback) || feedback.Trim().Length > 4000)) return ActivityChangeResult.InvalidFeedback;
            if (action == ActivityReviewAction.Approve && MissingFields(activity).Count > 0) return ActivityChangeResult.Incomplete;

            activity.ReviewStatus = action == ActivityReviewAction.Approve ? ActivityReviewStatus.Approved : ActivityReviewStatus.ChangesRequested;
            activity.ReviewedAt = now;
            activity.ReviewedByUserId = userId;
            activity.ReviewFeedback = action == ActivityReviewAction.RequestChanges ? feedback.Trim() : null;
        }

        activity.ReviewVersion++;
        activity.UpdatedAt = now;
        return ActivityChangeResult.Success;
    }

    public static void ContentChanged(Activity activity)
    {
        if (activity.ReviewStatus == ActivityReviewStatus.Approved || activity.ReviewStatus == ActivityReviewStatus.Submitted)
        {
            activity.ReviewStatus = ActivityReviewStatus.Draft;
            activity.SubmittedAt = null;
        }

        activity.ReviewVersion++;
        activity.UpdatedAt = DateTime.UtcNow;
    }
}

using DS.Domain.Models;

namespace DS.Domain.DTOs
{
    public class ActivityDto
    {
        public ActivityDto() { }

        public ActivityDto(Activity model)
        {
            Id = model.Id;
            Name = model.Name;
            Budget = model.Budget?.Budget ?? 0;
            Catalog = model.Catalog != null ? new CatalogDataDto(model.Catalog) : null;
            ReviewStatus = model.ReviewStatus.ToString();
            ReviewVersion = model.ReviewVersion;
            SubmittedAt = model.SubmittedAt;
            ReviewedAt = model.ReviewedAt;
            ReviewedByName = model.ReviewedBy?.GetFullName();
            ReviewFeedback = model.ReviewFeedback;
            ReviewMissingFields = ActivityReview.MissingFields(model);
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int Budget { get; set; }
        public CatalogDataDto Catalog { get; set; }
        public string ReviewStatus { get; set; }
        public int ReviewVersion { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewedByName { get; set; }
        public string ReviewFeedback { get; set; }
        public List<string> ReviewMissingFields { get; set; }
        public bool CanEdit { get; set; }
    }

    public class ActivityUpdateDto
    {
        public string Name { get; set; }
        public CatalogDataDto Catalog { get; set; }
        public int? ReviewVersion { get; set; }
    }

    public class ActivityReviewRequestDto
    {
        public int? ReviewVersion { get; set; }
        public string Feedback { get; set; }
    }

    public class CatalogDataDto
    {
        public CatalogDataDto() { }

        public CatalogDataDto(CatalogData model)
        {
            Id = model.Id;
            Name = model.Name;
            Summary = model.Summary;
            Description = model.Description;
            Image = model.ImageFile != null ? new ImageReferenceDto(model.ImageFile) : null;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public ImageReferenceDto Image { get; set; }
    }

    public class ActivityTeamDto
    {
        public ActivityTeamDto() { }

        public ActivityTeamDto(ActivityTeam model)
        {
            Id = model.Id;
            Name = model.Name;
            Members = model.Memberships?.Select(m => new ActivityTeamMemberDto(m)).ToList() ?? [];
            Activities = model.Activities?.Select(a => new ActivityDto(a)).ToList() ?? [];
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public List<ActivityTeamMemberDto> Members { get; set; }
        public List<ActivityDto> Activities { get; set; }
    }

    public class ActivityTeamMemberDto
    {
        public ActivityTeamMemberDto() { }

        public ActivityTeamMemberDto(ActivityTeamMembership model)
        {
            UserId = model.User.Id;
            Name = model.User.GetFullName();
            Email = model.User.Email ?? string.Empty;
            IsAdmin = model.IsAdmin;
        }

        public string UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class ActivityTeamMembershipDto
    {
        public string UserId { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class ActivityTeamInviteDto
    {
        public string Email { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class ActivityTeamInviteLinkDto
    {
        public string Link { get; set; }
        public string Email { get; set; }
    }

    public class ActivityFileDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Url { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ActivityFileCreateDto
    {
        public string Title { get; set; }
        public string Kind { get; set; }
        public string FolderId { get; set; }
    }
}
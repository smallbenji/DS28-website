using DS.Models;

namespace DS.DTOs
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
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int Budget { get; set; }
        public CatalogDataDto Catalog { get; set; }
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
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
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
}
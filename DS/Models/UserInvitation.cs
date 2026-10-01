namespace DS.Models;

public class UserInvitation : IAuditableEntity, ISoftDeleteable
{
    public int Id { get; set; }
    public Guid InvitationId { get; set; }
    public string Email { get; set; }
    public List<string> Roles { get; set; } = [];
    public bool Used { get; set; } = false;
    public int? ActivityTeamId { get; set; }
    public bool IsAdmin { get; set; }
    public int? GroupId { get; set; }
    public Group Group { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

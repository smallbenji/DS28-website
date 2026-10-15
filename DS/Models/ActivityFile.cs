namespace DS.Models;

public class ActivityFile : IAuditableEntity, ISoftDeleteable
{
    public int Id { get; set; }
    public string DriveFileId { get; set; }
    public string Title { get; set; }

    public int ActivityId { get; set; }
    public Activity Activity { get; set; }

    public string CreatedByUserId { get; set; }
    public User CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

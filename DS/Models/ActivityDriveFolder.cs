namespace DS.Models;

public class ActivityDriveFolder : IAuditableEntity
{
    public int ActivityId { get; set; }
    public Activity Activity { get; set; }

    public string DriveFolderId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

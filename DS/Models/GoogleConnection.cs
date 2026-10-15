namespace DS.Models;

public class GoogleConnection : IAuditableEntity
{
    public int Id { get; set; }
    public string AccountEmail { get; set; }
    public string RefreshToken { get; set; }

    public string ConnectedByUserId { get; set; }
    public User ConnectedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

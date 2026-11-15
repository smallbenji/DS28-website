namespace DS.Domain.Models;

public class UserAccessToken
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }
    public string Name { get; set; }
    public string TokenHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

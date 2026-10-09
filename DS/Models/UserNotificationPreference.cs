namespace DS.Models;

public class UserNotificationPreference
{
    public string UserId { get; set; }
    public User User { get; set; }
    public NotificationType NotificationType { get; set; }
}

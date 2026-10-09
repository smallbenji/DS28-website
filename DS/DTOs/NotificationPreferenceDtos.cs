namespace DS.DTOs
{
    public class NotificationPreferenceDto
    {
        public string Type { get; set; } = string.Empty;
        public bool CanSubscribe { get; set; }
        public bool Subscribed { get; set; }
    }

    public class UpdateNotificationPreferencesDto
    {
        public List<string> Types { get; set; } = [];
    }
}

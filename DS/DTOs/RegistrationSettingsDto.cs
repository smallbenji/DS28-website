using System.Text.Json.Serialization;

namespace DS.DTOs
{
    public class RegistrationSettingsDto
    {
        [JsonRequired]
        public bool IsPreSignupOpen { get; set; }
    }
}

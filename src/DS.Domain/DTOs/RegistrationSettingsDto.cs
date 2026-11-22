namespace DS.Domain.DTOs
{
    public class RegistrationSettingsDto
    {
        public bool? IsPreSignupOpen { get; set; }
        public bool? IsSignupOpen { get; set; }
        public bool? IsActivityWishRoundOpen { get; set; }
        public bool? IsActivitySignupOpen { get; set; }
    }
}

namespace DS.DTOs
{
    public class ResetPasswordLinkDto
    {
        public string Link { get; set; }
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }
}

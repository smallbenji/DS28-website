namespace DS.DTOs
{
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class TwoFactorLoginDto
    {
        public string TwoFactorCode { get; set; } = string.Empty;
        public bool RememberMachine { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
    }

    public class RecoveryCodeLoginDto
    {
        public string RecoveryCode { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
    }

    public class AuthResultDto
    {
        public bool RequiresTwoFactor { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
    }
}

namespace DS.DTOs
{
    public class TwoFactorStatusDto
    {
        public bool TwoFactorEnabled { get; set; }
        public int RecoveryCodesLeft { get; set; }
    }

    public class TwoFactorSetupDto
    {
        public string AuthenticatorUri { get; set; }
        public string ManualEntryKey { get; set; }
    }

    public class EnableTwoFactorDto
    {
        public string Code { get; set; }
    }

    public class EnableTwoFactorResultDto
    {
        public List<string> RecoveryCodes { get; set; }
    }

    public class DisableTwoFactorDto
    {
        public string Password { get; set; }
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class UpdateNameDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}

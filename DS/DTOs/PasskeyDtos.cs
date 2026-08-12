namespace DS.DTOs
{
    public class PasskeyOptionsDto
    {
        public string OptionsJson { get; set; }
    }

    public class PasskeyAttestationRequestDto
    {
        public string CredentialJson { get; set; }
        public string Name { get; set; }
    }

    public class PasskeyAssertionRequestDto
    {
        public string CredentialJson { get; set; }
        public bool RememberMachine { get; set; }
        public string ReturnUrl { get; set; }
        public string UserId { get; set; }
    }

    public class PasskeyDto
    {
        public string Id { get; set; }          // base64url credential id
        public string Name { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string[] Transports { get; set; }
        public bool IsBackedUp { get; set; }
    }

    public class PasskeyCreateOptionsDto
    {
        public string DisplayName { get; set; }
    }

}


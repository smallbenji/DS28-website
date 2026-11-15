namespace DS.Domain.DTOs
{
    public class AccessTokenDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class AccessTokenCreateDto
    {
        public string Name { get; set; }
        public int? ExpiresInDays { get; set; }
    }

    public class AccessTokenCreatedDto : AccessTokenDto
    {
        public string Token { get; set; }
    }
}

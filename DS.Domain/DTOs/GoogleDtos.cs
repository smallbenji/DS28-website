namespace DS.Domain.DTOs
{
    public class GoogleConnectionStatusDto
    {
        public bool Configured { get; set; }
        public bool Connected { get; set; }
        public string AccountEmail { get; set; }
    }
}

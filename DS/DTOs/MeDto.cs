namespace DS.DTOs
{
    public class MeDto
    {
        public bool IsAuthenticated { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = [];
        public List<string> AppRoles { get; set; } = [];
    }
}

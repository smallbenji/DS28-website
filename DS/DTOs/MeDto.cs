namespace DS.DTOs
{
    public class MeDto
    {
        public string Name { get; set; }
        public List<string> Roles { get; set; } = [];
        public List<string> AppRoles { get; set; } = [];
    }
}

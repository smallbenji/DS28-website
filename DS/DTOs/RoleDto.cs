namespace DS.DTOs
{
    public class RoleDto
    {
        public RoleDto() { }

        public RoleDto(Role role)
        {
            Id = role.Id;
            Name = role.Name;
        }

        public string Id { get; set; }
        public string Name { get; set; }
    }
}

namespace DS.DTOs
{
    public class GroupsDto
    {
        public GroupsDto() { }

        public GroupsDto(List<GroupDto> groups)
        {
            Groups = groups;
        }

        public List<GroupDto> Groups { get; set; }
        public Dictionary<string, List<UserDto>> Users { get; set; } = new();
    }
}

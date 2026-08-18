using DS.Models;

namespace DS.DTOs
{
    public class GroupDto
    {
        public GroupDto() { }
        public GroupDto(Group group, List<User> users = null)
        {
            Id = group.Id;
            Name = group.Name;
            District = group.District;
            Patrols = group.Patrols?.Select(p => new PatrolDto(p)).ToList() ?? [];
            Scouts = group.Scouts?.Select(s => new ScoutDto(s)).ToList() ?? [];
            Users = users?.Select(u => new GroupUserDto(u)).ToList() ?? [];
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public District District { get; set; }
        public List<PatrolDto> Patrols { get; set; } = [];
        public List<ScoutDto> Scouts { get; set; } = [];
        public List<GroupUserDto> Users { get; set; } = [];
    }

    public class GroupUserDto
    {
        public GroupUserDto(User user)
        {
            Id = user.Id;
            UserName = user.UserName ?? string.Empty;
            Email = user.Email ?? string.Empty;
            FirstName = user.FirstName ?? string.Empty;
            LastName = user.LastName ?? string.Empty;
        }

        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
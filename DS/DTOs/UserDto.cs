namespace DS.DTOs
{
    public class UserDto
    {
        public UserDto() { }

        public UserDto(User user)
        {
            Id = user.Id;
            UserName = user.UserName ?? string.Empty;
            Email = user.Email ?? string.Empty;
            Phone = user.PhoneNumber ?? string.Empty;
            FirstName = user.FirstName ?? string.Empty;
            LastName = user.LastName ?? string.Empty;
            Group = user.Group != null ? new GroupDto(user.Group) : null;
            LockoutEnd = user.LockoutEnd;
        }

        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public GroupDto Group { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public List<string> Roles { get; set; } = [];

        public void ApplyTo(User user)
        {
            user.UserName = string.IsNullOrWhiteSpace(UserName) ? user.UserName : UserName;
            user.Email = Email;
            user.FirstName = FirstName;
            user.LastName = LastName;
            user.PhoneNumber = Phone;
        }
    }
}
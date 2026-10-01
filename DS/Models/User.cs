using DS.DTOs;
using Microsoft.AspNetCore.Identity;

namespace DS.Models;

public class User : IdentityUser
{
    public User() {}
    public User(UserDto data)
    {
        UserName = string.IsNullOrWhiteSpace(data.UserName) ? data.Email : data.UserName;
        Email = data.Email;
        FirstName = data.FirstName;
        LastName = data.LastName;
    }

    public Group Group { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public bool HasEnabledAuthenticator { get; set; }

    public string GetFullName()
    {
        return string.Join(" ", FirstName, LastName);
    }
}

public class Role : IdentityRole
{
    
}

using DS.Models;
using Microsoft.AspNetCore.Identity;

namespace DS;

public class User : IdentityUser
{
    public Group Group { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public Boolean HasEnabledAuthenticator { get; set; }

    public string GetFullName()
    {
        return string.Join(" ", FirstName, LastName);
    }
}

public class Role : IdentityRole
{
    
}
using Microsoft.AspNetCore.Identity;

namespace DS;

public class User : IdentityUser
{
    public string GroupNumber { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public string GetFullName()
    {
        return string.Join(" ", FirstName, LastName);
    }
}

public class Role : IdentityRole
{
    
}
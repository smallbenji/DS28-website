using System.ComponentModel.DataAnnotations;

namespace DS.DTOs;

public class InviteGroupMemberDto
{
    [Required, EmailAddress]
    public string Email { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace DS.Domain.DTOs;

public class InviteGroupMemberDto
{
    [Required, EmailAddress]
    public string Email { get; set; }
}

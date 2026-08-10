using DS.Models;

namespace DS.DTOs
{
    public class UserInvitationDto
    {
        public UserInvitationDto() { }

        public UserInvitationDto(UserInvitation invitation)
        {
            Id = invitation.Id;
            InvitationId = invitation.InvitationId;
            Email = invitation.Email;
            Roles = invitation.Roles;
            Used = invitation.Used;
            ActivityTeamId = invitation.ActivityTeamId;
            IsAdmin = invitation.IsAdmin;
        }

        public int Id { get; set; }
        public Guid InvitationId { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public bool Used { get; set; }
        public int? ActivityTeamId { get; set; }
        public bool IsAdmin { get; set; }
    }
}

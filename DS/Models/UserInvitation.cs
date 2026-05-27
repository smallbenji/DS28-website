using System.ComponentModel.DataAnnotations;

namespace DS.Models
{
    public class UserInvitation
    {
        [Key]
        public int Id { get; set; }
        public Guid InvitationId { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public bool Used { get; set; } = false;
    }
}
using DS.Domain.Models;

namespace DS.Application.Services.Invitations
{
    public record GroupInvitationResult(InvitationOutcome Outcome, string Error = null, User NewUser = null);
}

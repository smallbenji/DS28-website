namespace DS.Application.Services.Invitations
{
    public enum InvitationOutcome
    {
        Success,
        NotFound,
        Invalid,
        Unauthorized
    }

    public record InvitationResult(InvitationOutcome Outcome, string Error = null, string UserId = null);
}

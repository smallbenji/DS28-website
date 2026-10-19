namespace DS.Application.Services.Organization
{
    public enum OrgOutcome
    {
        Success,
        Invalid,
        NotFound,
        Forbidden
    }

    public record OrgResult(OrgOutcome Outcome, object Value = null);
}

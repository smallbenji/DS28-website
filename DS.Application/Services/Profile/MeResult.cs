namespace DS.Application.Services.Profile
{
    public enum MeOutcome
    {
        Success,
        Invalid,
        NotFound,
        Forbidden
    }

    public record MeResult(MeOutcome Outcome, object Value = null);
}

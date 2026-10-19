namespace DS.Application.Services.Groups
{
    public enum GroupOutcome
    {
        Success,
        Invalid,
        NotFound,
        Forbidden,
        Conflict
    }

    public record GroupResult(GroupOutcome Outcome, object Value = null);
}

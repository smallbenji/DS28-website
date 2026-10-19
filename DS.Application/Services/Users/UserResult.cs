namespace DS.Application.Services.Users
{
    public enum UserOutcome
    {
        Success,
        Invalid,
        NotFound,
        Forbidden
    }

    public record UserResult(UserOutcome Outcome, object Value = null);
}

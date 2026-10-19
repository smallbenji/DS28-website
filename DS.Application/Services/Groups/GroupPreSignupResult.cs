using DS.Domain.Models;

namespace DS.Application.Services.Groups
{
    public enum GroupPreSignupOutcome
    {
        Success,
        NotFound,
        Conflict,
        Invalid
    }

    public record GroupPreSignupResult(GroupPreSignupOutcome Outcome, string Error = null, GroupPreSignupDetails Details = null, User NewUser = null);
}

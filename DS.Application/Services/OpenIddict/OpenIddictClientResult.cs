namespace DS.Application.Services.OpenIddict
{
    public enum OpenIddictClientOutcome
    {
        Success,
        Invalid,
        NotFound,
        Conflict
    }

    public record OpenIddictClientResult(OpenIddictClientOutcome Outcome, object Value = null);
}

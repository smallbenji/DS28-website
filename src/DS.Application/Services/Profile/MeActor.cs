namespace DS.Application.Services.Profile
{
    public record MeActor(string UserId, bool IsAuthenticated, bool IsImpersonating, List<string> Groups);
}

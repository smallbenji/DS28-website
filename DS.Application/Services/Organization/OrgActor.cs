namespace DS.Application.Services.Organization
{
    public record OrgActor(string UserId, bool IsSystemAdministrator, bool CanView);
}

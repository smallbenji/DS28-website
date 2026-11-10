namespace DS.Application.Services.Search
{
    public record SearchActor(string UserId, bool CanViewBoards, bool CanSeeAllBoards, bool CanSeeAllGroups, bool CanViewActivities, bool CanSeeAllActivities);
}

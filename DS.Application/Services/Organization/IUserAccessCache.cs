namespace DS.Application.Services.Organization
{
    public interface IUserAccessCache
    {
        void Evict(string userId);
    }
}

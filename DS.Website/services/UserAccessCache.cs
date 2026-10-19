using DS.Application.Services.Organization;
using Microsoft.Extensions.Caching.Memory;

namespace DS.Website.Services
{
    public class UserAccessCache(IMemoryCache memoryCache) : IUserAccessCache
    {
        public void Evict(string userId)
        {
            ClaimsTransformer.Evict(memoryCache, userId);
        }
    }
}

using DS.Domain.Models;
using DS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DS.Backend.Services
{
    public class UserActivityMiddleware(RequestDelegate next, IMemoryCache memoryCache, ILogger<UserActivityMiddleware> logger)
    {
        public static readonly TimeSpan UpdateInterval = TimeSpan.FromMinutes(5);
        private const string CachePrefix = "user-active:";

        public async Task InvokeAsync(HttpContext context, UserManager<User> userManager, DataDbContext dataDb)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = userManager.GetUserId(context.User);
                if (!string.IsNullOrEmpty(userId)) await RecordActivity(userId, dataDb, context.RequestAborted);
            }

            await next(context);
        }

        private async Task RecordActivity(string userId, DataDbContext dataDb, CancellationToken cancellationToken)
        {
            var cacheKey = CachePrefix + userId;
            if (memoryCache.TryGetValue(cacheKey, out _)) return;

            memoryCache.Set(cacheKey, true, UpdateInterval);

            var now = DateTimeOffset.UtcNow;
            var threshold = now - UpdateInterval;
            try
            {
                await dataDb.Users
                    .Where(user => user.Id == userId && (user.LastActiveAt == null || user.LastActiveAt < threshold))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.LastActiveAt, now), cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                memoryCache.Remove(cacheKey);
                logger.LogWarning(exception, "Kunne ikke gemme seneste aktivitet for bruger {UserId}", userId);
            }
        }
    }
}

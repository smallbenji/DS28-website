using Hangfire;
using OpenIddict.Abstractions;

namespace DS.Website.Services
{
    public class OpenIddictJobs(
        IOpenIddictTokenManager tokenManager,
        IOpenIddictAuthorizationManager authorizationManager,
        ILogger<OpenIddictJobs> logger)
    {
        [AutomaticRetry(Attempts = 3)]
        public async Task Prune()
        {
            var threshold = DateTimeOffset.UtcNow.AddDays(-7);
            var tokens = await tokenManager.PruneAsync(threshold);
            var authorizations = await authorizationManager.PruneAsync(threshold);

            logger.LogInformation("OpenIddict-oprydning fjernede {Tokens} tokens og {Authorizations} autoriseringer.", tokens, authorizations);
        }
    }
}

// IMemoryCache wrapper til at midlertidigt at opbevare passkey challenge tokens/state

using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace DS.Website.Services;

public sealed class PasskeyChallengeStore(IMemoryCache cache)
{
    private static readonly TimeSpan TTL = TimeSpan.FromMinutes(2);
    private static readonly string cache_prefix = "passkey_challenge";
    public string store(string state)
    {
        var token = Base64UrlTextEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        cache.Set($"{cache_prefix}::{token}", state, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TTL,
        });

        return token;
    }

    public string take(string token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        var cacheKey = $"{cache_prefix}::{token}";

        var state = cache.Get<string>(cacheKey);
        if (state is not null)
        {
            cache.Remove(cacheKey);
        }

        return state;
    }
}

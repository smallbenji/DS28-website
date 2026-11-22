using System.Security.Cryptography;
using System.Text;
using DS.Application.Data;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.AccessTokens
{
    public class AccessTokenService(IDataDbContext dataDb)
    {
        public const int DefaultExpiryDays = 90;
        private const string TokenPrefix = "ds28_";

        public async Task<ServiceResult<List<AccessTokenDto>>> GetTokens(string userId)
        {
            var tokens = await dataDb.AccessTokens
                .AsNoTracking()
                .Where(token => token.UserId == userId && token.RevokedAt == null)
                .OrderByDescending(token => token.CreatedAt)
                .Select(token => new AccessTokenDto
                {
                    Id = token.Id,
                    Name = token.Name,
                    CreatedAt = token.CreatedAt,
                    ExpiresAt = token.ExpiresAt,
                })
                .ToListAsync();

            return new ServiceResult<List<AccessTokenDto>>(ServiceOutcome.Success, tokens);
        }

        public async Task<ServiceResult<AccessTokenCreatedDto>> CreateToken(string userId, AccessTokenCreateDto data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Name)) return new ServiceResult(ServiceOutcome.Invalid, "Navn er påkrævet.");

            var name = data.Name.Trim();
            if (name.Length > 64) return new ServiceResult(ServiceOutcome.Invalid, "Navnet må højst være 64 tegn.");

            var days = data.ExpiresInDays ?? DefaultExpiryDays;
            if (days < 1 || days > 3650) return new ServiceResult(ServiceOutcome.Invalid, "Udløb skal være mellem 1 og 3650 dage.");

            var token = GenerateToken();
            var accessToken = new UserAccessToken
            {
                UserId = userId,
                Name = name,
                TokenHash = Hash(token),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(days),
            };

            dataDb.AccessTokens.Add(accessToken);
            await dataDb.SaveChangesAsync();

            return new ServiceResult<AccessTokenCreatedDto>(ServiceOutcome.Success, new AccessTokenCreatedDto
            {
                Id = accessToken.Id,
                Name = accessToken.Name,
                CreatedAt = accessToken.CreatedAt,
                ExpiresAt = accessToken.ExpiresAt,
                Token = token,
            });
        }

        public async Task<ServiceResult> RevokeToken(string userId, int id)
        {
            var token = await dataDb.AccessTokens
                .FirstOrDefaultAsync(candidate => candidate.Id == id && candidate.UserId == userId && candidate.RevokedAt == null);
            if (token == null) return new ServiceResult(ServiceOutcome.NotFound);

            token.RevokedAt = DateTime.UtcNow;
            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult<List<AccessTokenDto>>> GetActiveTokensForUser(string userId)
        {
            var userExists = await dataDb.Users.AnyAsync(user => user.Id == userId);
            if (!userExists) return new ServiceResult(ServiceOutcome.NotFound);

            var now = DateTime.UtcNow;
            var tokens = await dataDb.AccessTokens
                .AsNoTracking()
                .Where(token => token.UserId == userId
                    && token.RevokedAt == null
                    && (token.ExpiresAt == null || token.ExpiresAt > now))
                .OrderByDescending(token => token.CreatedAt)
                .Select(token => new AccessTokenDto
                {
                    Id = token.Id,
                    Name = token.Name,
                    CreatedAt = token.CreatedAt,
                    ExpiresAt = token.ExpiresAt,
                })
                .ToListAsync();

            return new ServiceResult<List<AccessTokenDto>>(ServiceOutcome.Success, tokens);
        }

        public async Task<ServiceResult<int>> RevokeAllTokens(string userId)
        {
            var userExists = await dataDb.Users.AnyAsync(user => user.Id == userId);
            if (!userExists) return new ServiceResult(ServiceOutcome.NotFound);

            var tokens = await dataDb.AccessTokens
                .Where(token => token.UserId == userId && token.RevokedAt == null)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var token in tokens)
            {
                token.RevokedAt = now;
            }

            await dataDb.SaveChangesAsync();

            return new ServiceResult<int>(ServiceOutcome.Success, tokens.Count);
        }

        public async Task<AccessTokenIdentity> Authenticate(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            var hash = Hash(token);
            var now = DateTime.UtcNow;
            var match = await dataDb.AccessTokens
                .AsNoTracking()
                .Where(candidate => candidate.TokenHash == hash
                    && candidate.RevokedAt == null
                    && (candidate.ExpiresAt == null || candidate.ExpiresAt > now))
                .Select(candidate => new
                {
                    candidate.UserId,
                    candidate.User.UserName,
                    candidate.User.LockoutEnd,
                })
                .FirstOrDefaultAsync();

            if (match == null || match.LockoutEnd > DateTimeOffset.UtcNow) return null;

            return new AccessTokenIdentity(match.UserId, match.UserName);
        }

        public static string Hash(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        }

        private static string GenerateToken()
        {
            var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            return TokenPrefix + secret;
        }
    }

    public record AccessTokenIdentity(string UserId, string UserName);
}

using DS.Domain;
using DS.Infrastructure.Data;
using DS.Application.Services.Google;
using Microsoft.Extensions.Logging;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace DS.Infrastructure.Google
{
    public class GoogleAuthService(
        IOptions<DSSettings> options,
        DataDbContext dataDb,
        IDataProtectionProvider dataProtection,
        ILogger<GoogleAuthService> logger) : IGoogleAuthService, IGoogleDriveClientFactory
    {
        private const string DriveScope = "https://www.googleapis.com/auth/drive";
        private const string FlowUserId = "ds28-hq";
        private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

        private readonly IDataProtector protector = dataProtection.CreateProtector("DS.GoogleDrive.RefreshToken");

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(options.Value.GoogleClientId)
            && !string.IsNullOrWhiteSpace(options.Value.GoogleClientSecret)
            && !string.IsNullOrWhiteSpace(options.Value.GoogleRedirectUri)
            && !string.IsNullOrWhiteSpace(options.Value.GoogleDriveBaseFolderId);

        public async Task<GoogleConnectionStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            var connection = await GetConnectionAsync(cancellationToken);

            return new GoogleConnectionStatusDto
            {
                Configured = IsConfigured,
                Connected = connection != null,
                AccountEmail = connection?.AccountEmail,
            };
        }

        public async Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
        {
            return await dataDb.GoogleConnections.AsNoTracking().AnyAsync(cancellationToken);
        }

        public string BuildAuthorizationUrl(string state)
        {
            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = options.Value.GoogleClientId,
                ["redirect_uri"] = options.Value.GoogleRedirectUri,
                ["response_type"] = "code",
                ["scope"] = DriveScope,
                ["access_type"] = "offline",
                ["prompt"] = "consent",
                ["state"] = state,
            };

            var query = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

            return $"{AuthorizationEndpoint}?{query}";
        }

        public async Task ConnectAsync(string code, string userId, CancellationToken cancellationToken = default)
        {
            var flow = CreateFlow();
            var token = await flow.ExchangeCodeForTokenAsync(FlowUserId, code, options.Value.GoogleRedirectUri, cancellationToken);

            if (string.IsNullOrWhiteSpace(token.RefreshToken))
            {
                throw new InvalidOperationException("Google returnerede ikke et refresh-token. Prøv igen, og accepter adgangen til Google Drev.");
            }

            var accountEmail = await GetAccountEmailAsync(token, cancellationToken);

            var existing = await dataDb.GoogleConnections.ToListAsync(cancellationToken);
            dataDb.GoogleConnections.RemoveRange(existing);

            dataDb.GoogleConnections.Add(new GoogleConnection
            {
                AccountEmail = accountEmail,
                RefreshToken = protector.Protect(token.RefreshToken),
                ConnectedByUserId = userId,
            });

            await dataDb.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Google Drev blev forbundet.");
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            var existing = await dataDb.GoogleConnections.ToListAsync(cancellationToken);

            if (existing.Count == 0) return;

            dataDb.GoogleConnections.RemoveRange(existing);
            await dataDb.SaveChangesAsync(cancellationToken);
        }

        public async Task<DriveService> GetDriveServiceAsync(CancellationToken cancellationToken = default)
        {
            var connection = await GetConnectionAsync(cancellationToken);

            if (connection == null)
            {
                throw new InvalidOperationException("Google Drev er ikke forbundet.");
            }

            string refreshToken;
            try
            {
                refreshToken = protector.Unprotect(connection.RefreshToken);
            }
            catch (CryptographicException ex)
            {
                logger.LogError(ex, "Refresh-tokenet for Google Drev kunne ikke dekrypteres.");
                throw new InvalidOperationException("Google Drev-forbindelsen skal fornyes.", ex);
            }

            var flow = CreateFlow();
            var token = await flow.RefreshTokenAsync(FlowUserId, refreshToken, cancellationToken);
            token.RefreshToken ??= refreshToken;

            var credential = new UserCredential(flow, FlowUserId, token);

            return new DriveService(new BaseClientService.Initializer
            {
                ApplicationName = "DS28 HQ",
                HttpClientInitializer = credential,
            });
        }

        private async Task<string> GetAccountEmailAsync(TokenResponse token, CancellationToken cancellationToken)
        {
            var credential = new UserCredential(CreateFlow(), FlowUserId, token);

            var service = new DriveService(new BaseClientService.Initializer
            {
                ApplicationName = "DS28 HQ",
                HttpClientInitializer = credential,
            });

            var request = service.About.Get();
            request.Fields = "user(emailAddress,displayName)";

            var about = await request.ExecuteAsync(cancellationToken);

            return about.User?.EmailAddress;
        }

        private async Task<GoogleConnection> GetConnectionAsync(CancellationToken cancellationToken)
        {
            return await dataDb.GoogleConnections.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        }

        private GoogleAuthorizationCodeFlow CreateFlow()
        {
            return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = options.Value.GoogleClientId,
                    ClientSecret = options.Value.GoogleClientSecret,
                },
                Scopes = [DriveScope],
            });
        }
    }
}

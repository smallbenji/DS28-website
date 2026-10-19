using System.Security.Cryptography;
using System.Text.Json;
using DS.Application.Services.OpenIddict;
using DS.Domain.DTOs;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace DS.Infrastructure.OpenIddict
{
    public class OpenIddictClientService(IOpenIddictApplicationManager applications) : IOpenIddictClientService
    {
        private static readonly string[] EditableScopes = [Scopes.Email, Scopes.Profile, Scopes.Roles];

        public async Task<OpenIddictClientResult> GetClients(CancellationToken cancellationToken)
        {
            var clients = new List<OpenIddictClientDto>();
            await foreach (var application in applications.ListAsync(cancellationToken: cancellationToken))
            {
                clients.Add(await GetClientAsync(application, cancellationToken));
            }

            return new OpenIddictClientResult(OpenIddictClientOutcome.Success, clients.OrderBy(client => client.ClientId).ToList());
        }

        public async Task<OpenIddictClientResult> CreateClient(OpenIddictClientUpdateDto data, CancellationToken cancellationToken)
        {
            var error = ValidateClient(data);
            if (error != null) return new OpenIddictClientResult(OpenIddictClientOutcome.Invalid, error);

            if (await applications.FindByClientIdAsync(data.ClientId.Trim(), cancellationToken) != null) return new OpenIddictClientResult(OpenIddictClientOutcome.Conflict, "Client ID er allerede i brug.");

            return await ExecuteChangeAsync(async () =>
            {
                var descriptor = new OpenIddictApplicationDescriptor
                {
                    ApplicationType = ApplicationTypes.Web,
                    ConsentType = ConsentTypes.Implicit
                };
                ApplyClient(descriptor, data);
                descriptor.Permissions.UnionWith([
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code
                ]);

                var secret = data.ClientType == ClientTypes.Confidential ? GenerateSecret() : null;
                descriptor.ClientSecret = secret;
                var application = await applications.CreateAsync(descriptor, cancellationToken);

                return new OpenIddictClientResult(OpenIddictClientOutcome.Success, new OpenIddictClientSavedDto
                {
                    Client = await GetClientAsync(application, cancellationToken),
                    ClientSecret = secret
                });
            });
        }

        public async Task<OpenIddictClientResult> UpdateClient(string id, OpenIddictClientUpdateDto data, CancellationToken cancellationToken)
        {
            var error = ValidateClient(data);
            if (error != null) return new OpenIddictClientResult(OpenIddictClientOutcome.Invalid, error);

            var application = await applications.FindByIdAsync(id, cancellationToken);
            if (application == null) return new OpenIddictClientResult(OpenIddictClientOutcome.NotFound);

            var duplicate = await applications.FindByClientIdAsync(data.ClientId.Trim(), cancellationToken);
            if (duplicate != null && await applications.GetIdAsync(duplicate, cancellationToken) != id) return new OpenIddictClientResult(OpenIddictClientOutcome.Conflict, "Client ID er allerede i brug.");

            return await ExecuteChangeAsync(async () =>
            {
                var descriptor = new OpenIddictApplicationDescriptor();
                await applications.PopulateAsync(descriptor, application, cancellationToken);
                var oldType = descriptor.ClientType;
                ApplyClient(descriptor, data);

                string secret = null;
                if (data.ClientType == ClientTypes.Public)
                {
                    descriptor.ClientSecret = null;
                }
                else if (oldType != ClientTypes.Confidential)
                {
                    secret = GenerateSecret();
                    descriptor.ClientSecret = secret;
                }

                await applications.UpdateAsync(application, descriptor, cancellationToken);

                return new OpenIddictClientResult(OpenIddictClientOutcome.Success, new OpenIddictClientSavedDto
                {
                    Client = await GetClientAsync(application, cancellationToken),
                    ClientSecret = secret
                });
            });
        }

        public async Task<OpenIddictClientResult> RotateSecret(string id, CancellationToken cancellationToken)
        {
            var application = await applications.FindByIdAsync(id, cancellationToken);
            if (application == null) return new OpenIddictClientResult(OpenIddictClientOutcome.NotFound);

            if (await applications.GetClientTypeAsync(application, cancellationToken) != ClientTypes.Confidential) return new OpenIddictClientResult(OpenIddictClientOutcome.Invalid, "Public-klienter har ikke et client secret.");

            return await ExecuteChangeAsync(async () =>
            {
                var secret = GenerateSecret();
                await applications.UpdateAsync(application, secret, cancellationToken);

                return new OpenIddictClientResult(OpenIddictClientOutcome.Success, new { clientSecret = secret });
            });
        }

        public async Task<OpenIddictClientResult> DeleteClient(string id, CancellationToken cancellationToken)
        {
            var application = await applications.FindByIdAsync(id, cancellationToken);
            if (application == null) return new OpenIddictClientResult(OpenIddictClientOutcome.NotFound);

            return await ExecuteChangeAsync(async () =>
            {
                await applications.DeleteAsync(application, cancellationToken);

                return new OpenIddictClientResult(OpenIddictClientOutcome.Success);
            });
        }

        private async Task<OpenIddictClientDto> GetClientAsync(object application, CancellationToken cancellationToken)
        {
            var permissions = await applications.GetPermissionsAsync(application, cancellationToken);
            var requirements = await applications.GetRequirementsAsync(application, cancellationToken);
            var properties = await applications.GetPropertiesAsync(application, cancellationToken);
            return new OpenIddictClientDto
            {
                Id = await applications.GetIdAsync(application, cancellationToken),
                ClientId = await applications.GetClientIdAsync(application, cancellationToken),
                DisplayName = await applications.GetDisplayNameAsync(application, cancellationToken),
                ClientType = await applications.GetClientTypeAsync(application, cancellationToken),
                RedirectUris = [.. await applications.GetRedirectUrisAsync(application, cancellationToken)],
                Scopes = [Scopes.OpenId, .. EditableScopes.Where(scope => permissions.Contains(Permissions.Prefixes.Scope + scope))],
                RequiredRoles = OpenIddictClientPolicy.ReadRequiredRoles(properties),
                RequirePkce = requirements.Contains(Requirements.Features.ProofKeyForCodeExchange)
            };
        }

        private static string ValidateClient(OpenIddictClientUpdateDto data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.ClientId) || data.ClientId.Trim().Length > 100
                || data.ClientId.Any(char.IsWhiteSpace)) return "Client ID skal angives, være højst 100 tegn og uden mellemrum.";

            if (string.IsNullOrWhiteSpace(data.DisplayName) || data.DisplayName.Trim().Length > 200) return "Navn skal angives og være højst 200 tegn.";

            if (data.ClientType != ClientTypes.Public && data.ClientType != ClientTypes.Confidential) return "Vælg public eller confidential som klienttype.";

            if (data.ClientType == ClientTypes.Public && !data.RequirePkce) return "Public-klienter skal bruge PKCE.";

            if (data.RedirectUris == null || data.RedirectUris.Length == 0 || data.RedirectUris.Length > 20) return "Angiv mellem 1 og 20 redirect-URL'er.";

            foreach (var value in data.RedirectUris)
            {
                if (string.IsNullOrWhiteSpace(value) || value.Length > 2048 || value.Contains('*')
                    || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
                    || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo)
                    || (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback))) return "Redirect-URL'er skal være absolutte HTTPS-URL'er (HTTP tillades på loopback), uden fragment, brugernavn eller wildcard.";
            }

            if (data.Scopes == null || data.Scopes.Any(scope => scope != Scopes.OpenId && !EditableScopes.Contains(scope))) return "Vælg kun scopes: openid, profile, email og roles.";

            if (!OpenIddictClientPolicy.IsValid(data.RequiredRoles)) return "Vælg mindst én gyldig AppRoles-rettighed til login.";

            return null;
        }

        private static void ApplyClient(OpenIddictApplicationDescriptor descriptor, OpenIddictClientUpdateDto data)
        {
            descriptor.ClientId = data.ClientId.Trim();
            descriptor.DisplayName = data.DisplayName.Trim();
            descriptor.ClientType = data.ClientType;
            descriptor.Properties[OpenIddictClientPolicy.RequiredRolesProperty] = JsonSerializer.SerializeToElement(data.RequiredRoles.Distinct(StringComparer.Ordinal).ToArray());
            descriptor.RedirectUris.Clear();
            descriptor.RedirectUris.UnionWith(data.RedirectUris.Select(value => new Uri(value, UriKind.Absolute)));

            foreach (var scope in EditableScopes)
            {
                descriptor.Permissions.Remove(Permissions.Prefixes.Scope + scope);
                if (data.Scopes.Contains(scope)) descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope);
            }

            descriptor.Requirements.Remove(Requirements.Features.ProofKeyForCodeExchange);
            if (data.RequirePkce) descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
        }

        private static string GenerateSecret()
        {
            return Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        }

        private async Task<OpenIddictClientResult> ExecuteChangeAsync(Func<Task<OpenIddictClientResult>> change)
        {
            try
            {
                return await change();
            }
            catch (OpenIddictExceptions.ValidationException)
            {
                return new OpenIddictClientResult(OpenIddictClientOutcome.Invalid, "Klientens indstillinger kunne ikke valideres. Kontrollér Client ID, klienttype og redirect-URL'er.");
            }
            catch (OpenIddictExceptions.ConcurrencyException)
            {
                return new OpenIddictClientResult(OpenIddictClientOutcome.Conflict, "Klienten er ændret af en anden. Genindlæs listen, og prøv igen.");
            }
            catch (DbUpdateException)
            {
                return new OpenIddictClientResult(OpenIddictClientOutcome.Conflict, "Klienten kunne ikke gemmes. Genindlæs listen, og kontrollér, at Client ID er unikt.");
            }
        }
    }
}

using DS;
using DS.HQ.Controllers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using NETCore.Keycloak.Client.HttpClients.Implementation;
using NETCore.Keycloak.Client.Models.Groups;
using NETCore.Keycloak.Client.Models.Users;
using Zitadel.Api;
using Zitadel.Credentials;
using Zitadel.Management.V1;
using Zitadel.User.V1;
using ZitadelUser = Zitadel.User.V1.User;

namespace DS.HQ
{
    public class ZitadelHelper : IKeycloakHelper
    {
        private readonly DSSettings dsSettings;
        private readonly HQSettings hqSettings;
        private readonly ZitadelSettings zitadelSettings;
        private readonly DataDbContext dataDb;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ServiceAccount serviceAccount;
        private readonly ManagementService.ManagementServiceClient client;

        public ZitadelHelper(IOptions<DSSettings> options, IOptions<HQSettings> hQOptions, IOptions<ZitadelSettings> zitadelOptions, DataDbContext dataDb, IHttpClientFactory httpClientFactory)
        {
            dsSettings = options.Value;
            hqSettings = hQOptions.Value;
            zitadelSettings = zitadelOptions.Value;
            this.dataDb = dataDb;
            this.httpClientFactory = httpClientFactory;

            serviceAccount = ServiceAccount.LoadFromJsonString(zitadelSettings.ServiceAccountJson);
            client = Clients.ManagementService(new Clients.Options(
                zitadelSettings.Endpoint,
                ITokenProvider.ServiceAccount(
                    zitadelSettings.Endpoint,
                    serviceAccount,
                    new ServiceAccount.AuthOptions { ApiAccess = true })));
        }

        public KeycloakClient GetClient()
        {
            return null;
        }

        public async Task<string> GetToken()
        {
            return await serviceAccount.AuthenticateAsync(zitadelSettings.Endpoint, new ServiceAccount.AuthOptions { ApiAccess = true });
        }

        public async Task<List<DSUser>> GetUsers()
        {
            var users = (await client.ListUsersAsync(new ListUsersRequest
            {
                Query = new Zitadel.V1.ListQuery { Limit = 500 }
            })).Result.ToList();

            var grants = await GetUserGrants(users.Select(u => u.Id));

            var retval = new List<DSUser>();

            foreach (var user in users)
            {
                var usr = new DSUser
                {
                    User = ToKcUser(user),
                    Roles = grants.Where(g => g.UserId == user.Id && g.ProjectId == zitadelSettings.ProjectId).SelectMany(ToKcGroups).ToList()
                };

                var groupNumber = await GetGroupNumber(user.Id);

                if (groupNumber != null)
                {
                    usr.GroupNumber = groupNumber;
                    usr.Group = dataDb.Groups.FirstOrDefault(x => x.Id.ToString().Equals(groupNumber));
                }

                retval.Add(usr);
            }

            return retval;
        }

        public async Task<DSUser> GetUser(string id)
        {
            var user = (await client.GetUserByIDAsync(new GetUserByIDRequest { Id = id })).User;

            var usr = new DSUser
            {
                User = ToKcUser(user),
                Roles = (await GetUserGrants(new[] { id })).Where(g => g.ProjectId == zitadelSettings.ProjectId).SelectMany(ToKcGroups).ToList()
            };

            var groupNumber = await GetGroupNumber(id);

            if (groupNumber != null)
            {
                usr.GroupNumber = groupNumber;
                usr.Group = dataDb.Groups.FirstOrDefault(x => x.Id.ToString().Equals(groupNumber));
            }

            return usr;
        }

        public async Task CreateUser(DSUser data)
        {
            var request = new AddHumanUserRequest
            {
                UserName = data.User.UserName,
                Email = new AddHumanUserRequest.Types.Email
                {
                    Email_ = data.User.Email,
                    IsEmailVerified = true
                },
                Profile = new AddHumanUserRequest.Types.Profile
                {
                    FirstName = data.User.FirstName,
                    LastName = data.User.LastName
                },
                InitialPassword = data.User.Credentials?.FirstOrDefault()?.Value
            };

            var userId = (await client.AddHumanUserAsync(request)).UserId;

            if (!string.IsNullOrEmpty(data.GroupNumber))
            {
                await SetGroupNumber(userId, data.GroupNumber);
            }

            if (data.User.Groups != null)
            {
                foreach (var role in data.User.Groups)
                {
                    await AddUserToGroup(userId, role);
                }
            }

            await RefreshUsers();
        }

        public async Task ResetUserPassword(string userId, string newPassword)
        {
            await client.SetHumanPasswordAsync(new SetHumanPasswordRequest
            {
                UserId = userId,
                Password = newPassword,
                NoChangeRequired = true
            });
        }

        public async Task DeleteUser(string id)
        {
            await client.RemoveUserAsync(new RemoveUserRequest { Id = id });

            await RefreshUsers();
        }

        public async Task UpdateUser(DSUser user)
        {
            var id = user.User.Id;

            await client.UpdateHumanProfileAsync(new UpdateHumanProfileRequest
            {
                UserId = id,
                FirstName = user.User.FirstName,
                LastName = user.User.LastName,
                DisplayName = $"{user.User.FirstName} {user.User.LastName}".Trim(),
                PreferredLanguage = "da"
            });

            if (!string.IsNullOrEmpty(user.User.Email))
            {
                await client.UpdateHumanEmailAsync(new UpdateHumanEmailRequest
                {
                    UserId = id,
                    Email = user.User.Email,
                    IsEmailVerified = true
                });
            }

            if (!string.IsNullOrEmpty(user.User.UserName))
            {
                await client.UpdateUserNameAsync(new UpdateUserNameRequest
                {
                    UserId = id,
                    UserName = user.User.UserName
                });
            }

            if (!string.IsNullOrEmpty(user.GroupNumber))
            {
                await SetGroupNumber(id, user.GroupNumber);
            }
            else
            {
                await RemoveGroupNumber(id);
            }

            await RefreshUsers();
        }

        public async Task<List<KcGroup>> GetGroups()
        {
            var roles = (await client.ListProjectRolesAsync(new ListProjectRolesRequest
            {
                ProjectId = zitadelSettings.ProjectId,
                Query = new Zitadel.V1.ListQuery { Limit = 500 }
            })).Result;

            return roles.Select(role => new KcGroup
            {
                Id = role.Key,
                Name = string.IsNullOrEmpty(role.DisplayName) ? role.Key : role.DisplayName,
                Path = role.Key
            }).ToList();
        }

        public async Task AddUserToGroup(string userId, string groupId)
        {
            var existing = (await GetUserGrants(new[] { userId })).FirstOrDefault(g => g.ProjectId == zitadelSettings.ProjectId);

            if (existing != null)
            {
                if (!existing.RoleKeys.Contains(groupId))
                {
                    var keys = new List<string>(existing.RoleKeys) { groupId };
                    var update = new UpdateUserGrantRequest
                    {
                        GrantId = existing.Id,
                        UserId = userId
                    };
                    update.RoleKeys.AddRange(keys);

                    await client.UpdateUserGrantAsync(update);
                }
            }
            else
            {
                await client.AddUserGrantAsync(new AddUserGrantRequest
                {
                    ProjectId = zitadelSettings.ProjectId,
                    UserId = userId,
                    RoleKeys = { groupId }
                });
            }

            await RefreshUsers();
        }

        public async Task RemoveUserFromGroup(string userId, string groupId)
        {
            var existing = (await GetUserGrants(new[] { userId })).FirstOrDefault(g => g.ProjectId == zitadelSettings.ProjectId && g.RoleKeys.Contains(groupId));

            if (existing == null)
            {
                return;
            }

            var remaining = existing.RoleKeys.Where(k => k != groupId).ToList();

            if (remaining.Count == 0)
            {
                await client.RemoveUserGrantAsync(new RemoveUserGrantRequest
                {
                    GrantId = existing.Id,
                    UserId = userId
                });
            }
            else
            {
                var update = new UpdateUserGrantRequest
                {
                    GrantId = existing.Id,
                    UserId = userId
                };
                update.RoleKeys.AddRange(remaining);

                await client.UpdateUserGrantAsync(update);
            }

            await RefreshUsers();
        }

        public async Task RefreshUsers()
        {
            KeycloakValidation.SetLastUpdate(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            if (hqSettings.UserRefreshUrl != null)
            {
                using var httpClient = httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("X-Internal-Api-Key", dsSettings.InternalApiKey);
                foreach (var site in hqSettings.UserRefreshUrl)
                {
                    await httpClient.GetAsync(site + "/refresh-users");
                }
            }
        }

        private async Task<List<UserGrant>> GetUserGrants(IEnumerable<string> userIds)
        {
            var userIdList = userIds as List<string> ?? userIds.ToList();

            if (userIdList.Count == 0)
            {
                return new List<UserGrant>();
            }

            return (await client.ListUserGrantsAsync(new ListUserGrantRequest
            {
                Query = new Zitadel.V1.ListQuery { Limit = 500 },
                Queries = { new UserGrantQuery { InUserIdsQuery = new UserGrantInUserIDsQuery { InUserIds = { userIdList } } } }
            })).Result.ToList();
        }

        private async Task<string> GetGroupNumber(string userId)
        {
            var metadata = (await client.ListUserMetadataAsync(new ListUserMetadataRequest { Id = userId })).Result;
            return metadata.FirstOrDefault(m => m.Key == "groupnumber")?.Value.ToStringUtf8();
        }

        private async Task SetGroupNumber(string userId, string groupNumber)
        {
            await client.SetUserMetadataAsync(new SetUserMetadataRequest
            {
                Id = userId,
                Key = "groupnumber",
                Value = ByteString.CopyFromUtf8(groupNumber)
            });
        }

        private async Task RemoveGroupNumber(string userId)
        {
            await client.RemoveUserMetadataAsync(new RemoveUserMetadataRequest
            {
                Id = userId,
                Key = "groupnumber"
            });
        }

        private static KcUser ToKcUser(ZitadelUser user)
        {
            return new KcUser
            {
                Id = user.Id,
                UserName = user.UserName,
                FirstName = user.Human?.Profile?.FirstName,
                LastName = user.Human?.Profile?.LastName,
                Email = user.Human?.Email?.Email_,
                EmailVerified = user.Human?.Email?.IsEmailVerified,
                Enabled = user.State == UserState.Active,
                CreatedTimestamp = user.Details?.CreationDate is { } creationDate
                    ? new DateTimeOffset(creationDate.ToDateTime(), TimeSpan.Zero).ToUnixTimeMilliseconds()
                    : 0
            };
        }

        private static List<KcGroup> ToKcGroups(UserGrant grant)
        {
            return grant.RoleKeys.Select(key => new KcGroup
            {
                Id = key,
                Name = key,
                Path = key
            }).ToList();
        }
    }
}

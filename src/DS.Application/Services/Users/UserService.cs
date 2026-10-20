using DS.Application.Data;
using DS.Application.Services.Email;
using DS.Application.Services.Organization;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Users
{
    public class UserService(IDataDbContext dataDb, UserManager<User> userManager, RoleManager<Role> roleManager, IUserAccessCache accessCache, EmailService emailService)
    {
        public async Task<ServiceResult<List<UserDto>>> GetUsers()
        {
            var users = await dataDb.Users
                .OrderBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .Include(x => x.Group)
                .Include(x => x.ProfilePicture)
                .ToListAsync();

            var positionsByUser = await dataDb.OrgPositionHolders
                .AsNoTracking()
                .Where(holder => holder.Position.DeletedAt == null)
                .Select(holder => new
                {
                    holder.UserId,
                    PositionId = holder.Position.Id,
                    PositionTitle = holder.Position.Title
                })
                .ToListAsync();

            var positionsLookup = positionsByUser
                .GroupBy(entry => entry.UserId)
                .ToDictionary(group => group.Key, group => group
                    .Select(entry => new OrgPositionDto
                    {
                        Id = entry.PositionId,
                        Title = entry.PositionTitle
                    })
                    .ToList());

            var roleNamesByUser = await (
                from userRole in dataDb.UserRoles
                join role in dataDb.Roles on userRole.RoleId equals role.Id
                select new { userRole.UserId, role.Name }
            ).AsNoTracking().ToListAsync();

            var rolesLookup = roleNamesByUser
                .GroupBy(entry => entry.UserId)
                .ToDictionary(group => group.Key, group => group.Select(entry => entry.Name).ToList());

            var retval = new List<UserDto>();
            foreach (var user in users)
            {
                var dto = new UserDto(user)
                {
                    Roles = rolesLookup.TryGetValue(user.Id, out var roles) ? roles : [],
                    Positions = positionsLookup.TryGetValue(user.Id, out var positions) ? positions : []
                };

                retval.Add(dto);
            }

            return new ServiceResult<List<UserDto>>(ServiceOutcome.Success, retval);
        }

        public async Task<ServiceResult> CreateUser(UserDto data)
        {
            var user = new User(data);

            if (data.Group != null)
            {
                var group = await dataDb.Groups.FindAsync(data.Group.Id);
                if (group == null) return new ServiceResult(ServiceOutcome.Invalid, "Gruppen findes ikke.");

                user.Group = group;
            }

            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded) return new ServiceResult(ServiceOutcome.Invalid, result.Errors);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> UpdateUser(UserDto data)
        {
            var user = await userManager.FindByIdAsync(data.Id);
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound);

            data.ApplyTo(user);

            if (data.Group != null)
            {
                var group = await dataDb.Groups.FindAsync(data.Group.Id);
                if (group == null) return new ServiceResult(ServiceOutcome.Invalid, "Gruppen findes ikke.");

                user.Group = group;
            }
            else
            {
                dataDb.Entry(user).Property("GroupId").CurrentValue = null;
            }

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) return new ServiceResult(ServiceOutcome.Invalid, result.Errors);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult<ImageReferenceDto>> UpdateProfilePicture(string currentUserId, string id, UpdateProfilePictureDto data)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound);

            if (data.Image == null)
            {
                user.ProfilePictureFileId = null;

                var clearResult = await userManager.UpdateAsync(user);
                if (!clearResult.Succeeded) return new ServiceResult(ServiceOutcome.Invalid, clearResult.Errors);

                return new ServiceResult(ServiceOutcome.Success);
            }


            var image = await dataDb.StoredFiles
                .FirstOrDefaultAsync(f => f.PublicId == data.Image.PublicId
                    && f.Purpose == FilePurpose.ProfilePicture
                    && f.Status == StoredFileStatus.Ready
                    && f.DeletedAt == null
                    && f.UploadedByUserId == currentUserId);
            if (image == null) return new ServiceResult(ServiceOutcome.Invalid, "Billedet findes ikke.");

            user.ProfilePictureFileId = image.Id;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) return new ServiceResult(ServiceOutcome.Invalid, result.Errors);

            return new ServiceResult<ImageReferenceDto>(ServiceOutcome.Success, new ImageReferenceDto(image));
        }

        public async Task<ServiceResult> AddUserToRole(string currentUserId, string role, string id)
        {
            var currentUser = await GetCurrentUserAsync(currentUserId);
            if (currentUser == null) return new ServiceResult(ServiceOutcome.Forbidden);

            var currentUserRoles = await userManager.GetRolesAsync(currentUser);
            if (!AppAccess.CanAssignRole(currentUserRoles, role)) return new ServiceResult(ServiceOutcome.Forbidden);

            var user = await userManager.FindByIdAsync(id);
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound);

            var result = await userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded) return new ServiceResult(ServiceOutcome.Invalid, result.Errors);

            accessCache.Evict(id);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> RemoveUserToRole(string currentUserId, string role, string id)
        {
            var currentUser = await GetCurrentUserAsync(currentUserId);
            if (currentUser == null) return new ServiceResult(ServiceOutcome.Forbidden);

            var currentUserRoles = await userManager.GetRolesAsync(currentUser);
            if (!AppAccess.CanAssignRole(currentUserRoles, role)) return new ServiceResult(ServiceOutcome.Forbidden);

            var user = await userManager.FindByIdAsync(id);
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound);

            var result = await userManager.RemoveFromRoleAsync(user, role);
            if (!result.Succeeded) return new ServiceResult(ServiceOutcome.Invalid, result.Errors);

            accessCache.Evict(id);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> DeleteUser(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound);

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded) return new ServiceResult(ServiceOutcome.Invalid, result.Errors);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> LockUser(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound);

            await userManager.SetLockoutEnabledAsync(user, true);
            var result = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            if (!result.Succeeded) return new ServiceResult(ServiceOutcome.Invalid, result.Errors);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> UnlockUser(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound);

            var result = await userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded) return new ServiceResult(ServiceOutcome.Invalid, result.Errors);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult<List<string>>> GetAssignableGroups(string currentUserId)
        {
            var currentUser = await GetCurrentUserAsync(currentUserId);
            if (currentUser == null) return new ServiceResult(ServiceOutcome.Forbidden);

            var currentUserRoles = await userManager.GetRolesAsync(currentUser);
            var assignable = Enum.GetValues<AppGroups>()
                .Where(g => AppAccess.CanAssignRole(currentUserRoles, g.ToString()))
                .Select(g => g.ToString())
                .ToList();

            return new ServiceResult<List<string>>(ServiceOutcome.Success, assignable);
        }

        public async Task<ServiceResult<List<RoleDto>>> GetGroups()
        {
            var retval = await roleManager.Roles
                .OrderBy(role => role.Name)
                .Select(role => new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name ?? string.Empty
                })
                .ToListAsync();

            return new ServiceResult<List<RoleDto>>(ServiceOutcome.Success, retval);
        }

        public async Task<ServiceResult<ResetPasswordLinkDto>> CreateResetPasswordLink(string requestOrigin, string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound);

            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            var link = $"{requestOrigin}/reset-password/{user.Id}?token={Uri.EscapeDataString(token)}";

            return new ServiceResult<ResetPasswordLinkDto>(ServiceOutcome.Success, new ResetPasswordLinkDto
            {
                Link = link,
                Email = user.Email
            });
        }

        public async Task<ServiceResult> InviteUser(string currentUserId, UserInvitationDto data)
        {
            var currentUser = await GetCurrentUserAsync(currentUserId);
            if (currentUser == null) return new ServiceResult(ServiceOutcome.Forbidden);

            var currentUserRoles = await userManager.GetRolesAsync(currentUser);
            var allowedRoles = data.Roles
                .Where(r => AppAccess.CanAssignRole(currentUserRoles, r))
                .ToList();
            if (allowedRoles.Count != data.Roles.Count)
            {
                return new ServiceResult(ServiceOutcome.Invalid, "Du har ikke tilladelse til at tildele nogle af de valgte roller.");
            }

            var newUser = await userManager.FindByEmailAsync(data.Email);
            if (newUser != null) return new ServiceResult(ServiceOutcome.Invalid, "Bruger findes allerede.");

            var invitation = new UserInvitation()
            {
                InvitationId = Guid.NewGuid(),
                Roles = allowedRoles,
                Email = data.Email
            };

            await dataDb.Invitations.AddAsync(invitation);
            await dataDb.SaveChangesAsync();

            emailService.QueueInvitationMail(invitation);

            return new ServiceResult(ServiceOutcome.Success);
        }

        private async Task<User> GetCurrentUserAsync(string userId)
        {
            if (userId == null) return null;

            return await userManager.FindByIdAsync(userId);
        }
    }
}

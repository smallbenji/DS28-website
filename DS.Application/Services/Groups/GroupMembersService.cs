using DS.Application.Services.Email;
using DS.Application.Data;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DS.Application.Services.Groups
{
    public class GroupMembersService(IDataDbContext dataDb, UserManager<User> userManager, EmailService emailService)
    {
        private Task<User> GetCurrentUserAsync(string userId)
        {
            return userManager.Users
                .Include(u => u.Group)
                .SingleOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<ServiceResult<object>> InviteAsync(string userId, InviteGroupMemberDto data)
        {
            var currentUser = await GetCurrentUserAsync(userId);
            if (currentUser?.Group == null) return new ServiceResult(ServiceOutcome.Forbidden);

            var email = data.Email.Trim();
            var existingUser = await userManager.Users
                .Include(u => u.Group)
                .SingleOrDefaultAsync(u =>
                    u.NormalizedEmail == userManager.NormalizeEmail(email));
            if (existingUser?.Group != null)
            {
                return new ServiceResult(ServiceOutcome.Invalid, "Brugeren er allerede tilknyttet en gruppe.");
            }

            var invitation = new UserInvitation
            {
                InvitationId = Guid.NewGuid(),
                Email = email,
                Roles = [],
                GroupId = currentUser.Group.Id
            };

            dataDb.Invitations.Add(invitation);
            await dataDb.SaveChangesAsync();

            emailService.QueueInvitationMail(invitation);

            return new ServiceResult<object>(ServiceOutcome.Success, new { Path = $"/group-invitation/{invitation.InvitationId}" });
        }

        public async Task<ServiceResult<object>> GetInvitationsAsync(string userId)
        {
            var currentUser = await GetCurrentUserAsync(userId);
            if (currentUser?.Group == null) return new ServiceResult(ServiceOutcome.Forbidden);

            return new ServiceResult<object>(ServiceOutcome.Success, await dataDb.Invitations
                .Where(i => i.GroupId == currentUser.Group.Id && !i.Used)
                .Select(i => new { i.InvitationId, i.Email })
                .ToListAsync());
        }

        public async Task<ServiceResult> RevokeAsync(string userId, Guid id)
        {
            var currentUser = await GetCurrentUserAsync(userId);
            if (currentUser?.Group == null) return new ServiceResult(ServiceOutcome.Forbidden);

            var updatedCount = await dataDb.Invitations
                .Where(i =>
                    i.GroupId == currentUser.Group.Id &&
                    i.InvitationId == id &&
                    !i.Used)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Used, true));

            return updatedCount == 0 ? new ServiceResult(ServiceOutcome.NotFound) : new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> RemoveAsync(string userId, string id)
        {
            var currentUser = await GetCurrentUserAsync(userId);
            if (currentUser?.Group == null) return new ServiceResult(ServiceOutcome.Forbidden);

            if (currentUser.Id == id)
            {
                return new ServiceResult(ServiceOutcome.Invalid, "Du kan ikke fjerne dig selv fra gruppen.");
            }

            await using var transaction = await dataDb.Database.BeginTransactionAsync();

            await dataDb.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM \"Groups\" WHERE \"Id\" = {currentUser.Group.Id} FOR UPDATE");
            if (!await userManager.Users.AnyAsync(u =>
                u.Id == currentUser.Id &&
                u.Group.Id == currentUser.Group.Id))
            {
                return new ServiceResult(ServiceOutcome.Forbidden);
            }

            var member = await userManager.Users
                .Include(u => u.Group)
                .SingleOrDefaultAsync(u =>
                    u.Id == id &&
                    u.Group.Id == currentUser.Group.Id);
            if (member == null)
            {
                return new ServiceResult(ServiceOutcome.NotFound, "Brugeren er ikke medlem af din gruppe.");
            }

            member.Group = null;
            dataDb.Entry(member).Property("GroupId").CurrentValue = null;
            await dataDb.SaveChangesAsync();
            await transaction.CommitAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }
    }
}

using DS.Application.Data;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Invitations
{
    public class GroupInvitationService(IDataDbContext dataDb, UserManager<User> userManager)
    {
        public async Task<GroupInvitationDetails> GetInvitationAsync(Guid id)
        {
            var invitation = await dataDb.Invitations
                .Include(i => i.Group)
                .SingleOrDefaultAsync(i =>
                    i.InvitationId == id &&
                    i.GroupId != null &&
                    !i.Used);
            if (invitation?.Group == null) return null;

            var existingUser = await userManager.FindByEmailAsync(invitation.Email) != null;

            return new GroupInvitationDetails(invitation.Email, invitation.Group.Name, existingUser);
        }

        public async Task<GroupInvitationResult> AcceptAsync(Guid id, UserInvitationCreationDto data, string currentUserId, bool isAuthenticated)
        {
            await using var transaction = await dataDb.Database.BeginTransactionAsync();

            var claimedCount = await dataDb.Invitations
                .Where(i => i.InvitationId == id && i.GroupId != null && !i.Used)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Used, true));

            if (claimedCount != 1) return new GroupInvitationResult(InvitationOutcome.NotFound, "Invitationen findes ikke eller er allerede brugt eller annulleret.");

            var invitation = await dataDb.Invitations
                .Include(i => i.Group)
                .SingleAsync(i => i.InvitationId == id);

            if (invitation.Group == null) return new GroupInvitationResult(InvitationOutcome.NotFound, "Gruppen findes ikke længere.");

            var user = await userManager.Users
                .Include(u => u.Group)
                .SingleOrDefaultAsync(u =>
                    u.NormalizedEmail == userManager.NormalizeEmail(invitation.Email));

            var isNewUser = user == null;
            if (!isNewUser)
            {
                if (currentUserId != user.Id)
                {
                    return new GroupInvitationResult(InvitationOutcome.Unauthorized, "Log ind med den inviterede email for at acceptere.");
                }

                var joinedCount = await userManager.Users
                    .Where(u => u.Id == user.Id && u.Group == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        u => EF.Property<int?>(u, "GroupId"),
                        invitation.GroupId));
                if (joinedCount != 1)
                {
                    return new GroupInvitationResult(InvitationOutcome.Invalid, "Du er allerede tilknyttet en gruppe.");
                }
            }
            else
            {
                if (isAuthenticated) return new GroupInvitationResult(InvitationOutcome.Invalid, "Log ud, før du opretter en konto med den inviterede email.");

                if (data == null ||
                    string.IsNullOrWhiteSpace(data.FirstName) ||
                    string.IsNullOrWhiteSpace(data.LastName) ||
                    string.IsNullOrWhiteSpace(data.Password))
                {
                    return new GroupInvitationResult(InvitationOutcome.Invalid, "Udfyld navn og adgangskode.");
                }

                user = new User
                {
                    FirstName = data.FirstName.Trim(),
                    LastName = data.LastName.Trim(),
                    Email = invitation.Email,
                    UserName = invitation.Email,
                    EmailConfirmed = true,
                    Group = invitation.Group
                };

                var result = await userManager.CreateAsync(user, data.Password);

                if (!result.Succeeded) return new GroupInvitationResult(InvitationOutcome.Invalid, string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            await transaction.CommitAsync();

            return new GroupInvitationResult(InvitationOutcome.Success, NewUser: isNewUser ? user : null);
        }
    }
}

using DS.Application.Data;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Invitations
{
    public class InvitationService(IDataDbContext dataDb, UserManager<User> userManager)
    {
        public async Task<UserInvitationDto> GetInvitation(string id)
        {
            Guid.TryParse(id, out var invitationId);
            var invitation = await dataDb.Invitations
                .FirstOrDefaultAsync(x => x.InvitationId == invitationId && x.GroupId == null);
            if (invitation == null) return null;

            return new UserInvitationDto(invitation);
        }

        public async Task<InvitationResult> CreateUser(UserInvitationCreationDto data, Guid id)
        {
            var invitation = await dataDb.Invitations
                .FirstOrDefaultAsync(x => x.InvitationId == id && x.GroupId == null);
            if (invitation == null) return new InvitationResult(InvitationOutcome.NotFound, "Invitation not found");

            if (invitation.Used) return new InvitationResult(InvitationOutcome.Invalid, "Invitation has already been used");

            if (string.IsNullOrWhiteSpace(data.Password) || data.Password.Length < 4)
            {
                return new InvitationResult(InvitationOutcome.Invalid, "Password must be at least 4 characters long");
            }

            if (string.IsNullOrWhiteSpace(data.FirstName)
                || string.IsNullOrWhiteSpace(data.LastName))
            {
                return new InvitationResult(InvitationOutcome.Invalid, "First name and last name are required");
            }

            var newUser = new User
            {
                FirstName = data.FirstName,
                LastName = data.LastName,
                UserName = (data.FirstName + data.LastName).Replace(" ", "").ToLower(),
                Email = invitation.Email,
            };

            var result = await userManager.CreateAsync(newUser, data.Password);
            if (!result.Succeeded) return new InvitationResult(InvitationOutcome.Invalid, "Fejl under oprettelse");

            var user = await userManager.FindByEmailAsync(invitation.Email);
            if (user != null)
            {
                await userManager.AddToRolesAsync(user, invitation.Roles);
            }

            if (invitation.ActivityTeamId != null && user != null)
            {
                dataDb.ActivityTeamMemberships.Add(new ActivityTeamMembership
                {
                    ActivityTeamId = invitation.ActivityTeamId.Value,
                    User = user,
                    IsAdmin = invitation.IsAdmin
                });
            }

            if (invitation.OrgPositionId != null && user != null)
            {
                dataDb.OrgPositionHolders.Add(new OrgPositionHolder
                {
                    PositionId = invitation.OrgPositionId.Value,
                    UserId = user.Id
                });
            }

            invitation.Used = true;

            dataDb.Invitations.Update(invitation);
            await dataDb.SaveChangesAsync();

            return new InvitationResult(InvitationOutcome.Success, UserId: user?.Id);
        }
    }
}

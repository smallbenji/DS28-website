using DS.Application.Data;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DS.Application.Services.Groups
{
    public class GroupPreSignupService(IDataDbContext dataDb, UserManager<User> userManager)
    {
        public async Task<GroupPreSignupResult> Lookup(int groupId)
        {
            if (!await dataDb.IsPreSignupOpenAsync(lockSettings: false))
            {
                return new GroupPreSignupResult(GroupPreSignupOutcome.Conflict, "Forhåndstilmeldingen er lukket.");
            }

            var group = await dataDb.Groups
                .AsNoTracking()
                .Include(g => g.PreSignup)
                .Where(g => g.Id == groupId)
                .SingleOrDefaultAsync();
            if (group == null)
            {
                return new GroupPreSignupResult(GroupPreSignupOutcome.NotFound, "Gruppen blev ikke fundet. Kontrollér gruppenummeret.");
            }

            var hasUsers = await dataDb.Users.AnyAsync(u => u.Group != null && u.Group.Id == groupId);
            if (hasUsers)
            {
                return new GroupPreSignupResult(GroupPreSignupOutcome.Conflict, "Gruppen er allerede forhåndstilmeldt. Log ind eller kontakt gruppens kontaktperson.");
            }

            return new GroupPreSignupResult(GroupPreSignupOutcome.Success, Details: new GroupPreSignupDetails(
                group.Id,
                group.Name,
                group.District,
                group.PreSignup != null ? new UpdateGroupPreSignupDto(group.PreSignup) : null));
        }

        public async Task<GroupPreSignupResult> Create(GroupPreSignupDto data)
        {
            await using var transaction = await dataDb.Database.BeginTransactionAsync();

            if (!await dataDb.IsPreSignupOpenAsync(lockSettings: true))
            {
                return new GroupPreSignupResult(GroupPreSignupOutcome.Conflict, "Forhåndstilmeldingen er lukket.");
            }

            await dataDb.LockGroupForUpdateAsync(data.GroupId);

            var group = await dataDb.Groups
                .Include(g => g.PreSignup)
                .SingleOrDefaultAsync(g => g.Id == data.GroupId);
            if (group == null) return new GroupPreSignupResult(GroupPreSignupOutcome.NotFound, "Gruppen blev ikke fundet. Kontrollér gruppenummeret.");

            var hasUsers = await dataDb.Users.AnyAsync(u => u.Group != null && u.Group.Id == group.Id);
            if (hasUsers) return new GroupPreSignupResult(GroupPreSignupOutcome.Conflict, "Gruppen er allerede forhåndstilmeldt. Log ind eller kontakt gruppens kontaktperson.");

            var user = new User
            {
                FirstName = data.FirstName.Trim(),
                LastName = data.LastName.Trim(),
                UserName = data.Email.Trim(),
                Email = data.Email.Trim(),
                EmailConfirmed = true,
                Group = group
            };

            try
            {
                var result = await userManager.CreateAsync(user, data.Password);
                if (!result.Succeeded)
                {
                    return new GroupPreSignupResult(GroupPreSignupOutcome.Invalid, string.Join(" ", result.Errors.Select(e => e.Description)));
                }

                if (group.PreSignup == null)
                {
                    dataDb.GroupPreSignups.Add(new GroupPreSignup
                    {
                        GroupId = group.Id,
                        Beaver = data.Beaver,
                        Wolf = data.Wolf,
                        Junior = data.Junior,
                        Trop = data.Trop,
                        Senior = data.Senior,
                        Rover = data.Rover,
                        Leader = data.Leader
                    });
                }
                else
                {
                    group.PreSignup.Beaver = data.Beaver;
                    group.PreSignup.Wolf = data.Wolf;
                    group.PreSignup.Junior = data.Junior;
                    group.PreSignup.Trop = data.Trop;
                    group.PreSignup.Senior = data.Senior;
                    group.PreSignup.Rover = data.Rover;
                    group.PreSignup.Leader = data.Leader;
                }

                await dataDb.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await transaction.RollbackAsync();

                return new GroupPreSignupResult(GroupPreSignupOutcome.Conflict, "Gruppen eller emailadressen er allerede registreret. Log ind, eller kontrollér oplysningerne.");
            }

            return new GroupPreSignupResult(GroupPreSignupOutcome.Success, NewUser: user);
        }
    }
}

using DS.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DS.Application.Data
{
    public interface IDataDbContext
    {
        DatabaseFacade Database { get; }
        EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
        DbSet<Group> Groups { get; }
        DbSet<Scout> Scouts { get; }
        DbSet<Patrol> Patrols { get; }
        DbSet<PatrolMembership> PatrolMemberships { get; }
        DbSet<GroupPreSignup> GroupPreSignups { get; }
        DbSet<RegistrationSettings> RegistrationSettings { get; }
        DbSet<Activity> Activities { get; }
        DbSet<ActivityTeam> ActivityTeams { get; }
        DbSet<ActivityTeamMembership> ActivityTeamMemberships { get; }
        DbSet<StoredFile> StoredFiles { get; }
        DbSet<ActivityFile> ActivityFiles { get; }
        DbSet<ActivityDriveFolder> ActivityDriveFolders { get; }
        DbSet<User> Users { get; }
        DbSet<Role> Roles { get; }
        DbSet<IdentityUserRole<string>> UserRoles { get; }
        DbSet<UserInvitation> Invitations { get; }
        DbSet<OrgPositionHolder> OrgPositionHolders { get; }
        DbSet<OrgPosition> OrgPositions { get; }
        DbSet<OrgPositionGroup> OrgPositionGroups { get; }
        DbSet<Board> Boards { get; }
        DbSet<BoardMember> BoardMembers { get; }
        DbSet<BoardColor> BoardColors { get; }
        DbSet<BoardColumn> BoardColumns { get; }
        DbSet<BoardCard> BoardCards { get; }
        DbSet<WorkItem> WorkItems { get; }
        DbSet<UserNotificationPreference> NotificationPreferences { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

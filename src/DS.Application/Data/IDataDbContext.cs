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
        ChangeTracker ChangeTracker { get; }
        EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
        DbSet<Group> Groups { get; }
        DbSet<Scout> Scouts { get; }
        DbSet<Patrol> Patrols { get; }
        DbSet<PatrolMembership> PatrolMemberships { get; }
        DbSet<GroupPreSignup> GroupPreSignups { get; }
        DbSet<HistoricPresignup> HistoricPresignups { get; }
        DbSet<HistoricSignup> HistoricSignups { get; }
        DbSet<RegistrationSettings> RegistrationSettings { get; }
        DbSet<CampDay> CampDays { get; }
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
        DbSet<BoardTag> BoardTags { get; }
        DbSet<BoardCardTag> BoardCardTags { get; }
        DbSet<BoardCardPullRequest> BoardCardPullRequests { get; }
        DbSet<Board> Boards { get; }
        DbSet<BoardMember> BoardMembers { get; }
        DbSet<BoardColor> BoardColors { get; }
        DbSet<BoardColumn> BoardColumns { get; }
        DbSet<BoardCard> BoardCards { get; }
        DbSet<WorkItem> WorkItems { get; }
        DbSet<WorkItemDependency> WorkItemDependencies { get; }
        DbSet<WorkItemAssignee> WorkItemAssignees { get; }
        DbSet<Material> Materials { get; }
        DbSet<MaterialOrder> MaterialOrders { get; }

        DbSet<UserNotificationPreference> NotificationPreferences { get; }
        DbSet<UserAccessToken> AccessTokens { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

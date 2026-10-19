using DS.Application.Data;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Organization
{
    public static class OrgPositionAccess
    {
        public static IQueryable<string> GetEffectiveGroups(IDataDbContext dataDb, string userId)
        {
            var ownGroups = dataDb.OrgPositionHolders
                .Where(holder => holder.UserId == userId && holder.Position.DeletedAt == null)
                .SelectMany(holder => holder.Position.Groups)
                .Select(group => group.GroupName);

            var inheritedGroups = dataDb.OrgPositionHolders
                .Where(holder => holder.UserId == userId
                    && holder.Position.DeletedAt == null
                    && holder.Position.PaForPosition != null
                    && holder.Position.PaForPosition.DeletedAt == null)
                .SelectMany(holder => holder.Position.PaForPosition.Groups)
                .Select(group => group.GroupName);

            return ownGroups.Concat(inheritedGroups).Distinct();
        }
    }
}

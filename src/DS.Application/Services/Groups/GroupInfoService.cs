using DS.Application.Data;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Groups;

public class GroupInfoService(IDataDbContext dataDb)
{
    public async Task<int> GetTotalPresignup()
    {
        return await dataDb.GroupPreSignups
            .Select(m => (int?)(m.Beaver + m.Wolf + m.Junior + m.Trop + m.Senior + m.Rover + m.Leader))
            .SumAsync() ?? 0;
    }

    public async Task<int> GetTotalSignups() => await dataDb.Scouts.CountAsync();

    public async Task<int> GetTotalGroups() => await dataDb.Groups.CountAsync();

    public async Task<int> GetTotalUsers() => await dataDb.Users.CountAsync();
}
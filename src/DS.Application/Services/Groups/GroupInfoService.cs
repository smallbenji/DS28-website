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

    public async Task<int?> GetLatestHistoricYear()
    {
        var presignupYear = await dataDb.HistoricPresignups.MaxAsync(row => (int?)row.CampYear);
        var signupYear = await dataDb.HistoricSignups.MaxAsync(row => (int?)row.CampYear);

        if (presignupYear == null) return signupYear;
        if (signupYear == null) return presignupYear;

        return Math.Max(presignupYear.Value, signupYear.Value);
    }

    public async Task<int?> GetHistoricPresignupTotal(int campYear)
    {
        var rows = dataDb.HistoricPresignups.Where(row => row.CampYear == campYear);
        if (!await rows.AnyAsync()) return null;

        return await rows.SumAsync(row => row.ParticipantCount);
    }

    public async Task<int?> GetHistoricSignupTotal(int campYear)
    {
        var rows = dataDb.HistoricSignups.Where(row => row.CampYear == campYear && row.DayIndex == 0);
        if (!await rows.AnyAsync()) return null;

        return await rows.SumAsync(row => row.ParticipantCount);
    }
}
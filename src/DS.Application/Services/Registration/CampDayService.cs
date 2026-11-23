using System.Data.Common;
using DS.Application.Data;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Registration
{
    public class CampDayService(IDataDbContext dataDb)
    {
        private const string DuplicateDateMessage = "Datoen er allerede en lejrdag.";

        public async Task<List<CampDayDto>> GetDays()
        {
            return await dataDb.CampDays
                .AsNoTracking()
                .OrderBy(day => day.Date)
                .Select(day => new CampDayDto
                {
                    Id = day.Id,
                    Date = day.Date,
                    Half = day.Half
                })
                .ToListAsync();
        }

        public async Task<ServiceResult<CampDayDto>> CreateDay(CampDayUpsertDto data)
        {
            var validation = Validate(data);
            if (validation != null) return validation;

            var day = new CampDay
            {
                Date = data.Date.Value,
                Half = data.Half.Value
            };

            dataDb.CampDays.Add(day);
            try
            {
                await dataDb.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (exception.InnerException is DbException { SqlState: "23505" })
            {
                dataDb.Entry(day).State = EntityState.Detached;
                return new ServiceResult(ServiceOutcome.Conflict, DuplicateDateMessage);
            }

            return new ServiceResult<CampDayDto>(ServiceOutcome.Success, ToDto(day));
        }

        public async Task<ServiceResult<CampDayDto>> UpdateDay(int id, CampDayUpsertDto data)
        {
            var validation = Validate(data);
            if (validation != null) return validation;

            var day = await dataDb.CampDays.SingleOrDefaultAsync(candidate => candidate.Id == id);
            if (day == null) return new ServiceResult(ServiceOutcome.NotFound, "Lejrdagen findes ikke.");

            var previousDate = day.Date;
            var previousHalf = day.Half;
            day.Date = data.Date.Value;
            day.Half = data.Half.Value;
            try
            {
                await dataDb.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (exception.InnerException is DbException { SqlState: "23505" })
            {
                day.Date = previousDate;
                day.Half = previousHalf;
                dataDb.Entry(day).State = EntityState.Unchanged;
                return new ServiceResult(ServiceOutcome.Conflict, DuplicateDateMessage);
            }

            return new ServiceResult<CampDayDto>(ServiceOutcome.Success, ToDto(day));
        }

        public async Task<ServiceResult> DeleteDay(int id)
        {
            var day = await dataDb.CampDays.SingleOrDefaultAsync(candidate => candidate.Id == id);
            if (day == null) return new ServiceResult(ServiceOutcome.NotFound, "Lejrdagen findes ikke.");

            var registeredScouts = await dataDb.ScoutCampDays.CountAsync(registration => registration.CampDayId == id && registration.Scout.DeletedAt == null);
            if (registeredScouts > 0) return new ServiceResult(ServiceOutcome.Conflict, $"{registeredScouts} {(registeredScouts == 1 ? "spejder er" : "spejdere er")} tilmeldt dagen, så den kan ikke slettes. Datoen kan stadig rettes.");

            var leftoverRegistrations = await dataDb.ScoutCampDays
                .IgnoreQueryFilters()
                .Where(registration => registration.CampDayId == id)
                .ToListAsync();
            dataDb.ScoutCampDays.RemoveRange(leftoverRegistrations);
            dataDb.CampDays.Remove(day);
            try
            {
                await dataDb.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (exception.InnerException is DbException { SqlState: "23503" })
            {
                dataDb.ChangeTracker.Clear();
                return new ServiceResult(ServiceOutcome.Conflict, "Der blev tilmeldt spejdere til dagen samtidig, så den kan ikke slettes.");
            }

            return new ServiceResult(ServiceOutcome.Success);
        }

        private static ServiceResult Validate(CampDayUpsertDto data)
        {
            if (data?.Date == null) return new ServiceResult(ServiceOutcome.Invalid, "Angiv en dato.");
            if (data.Half == null || !Enum.IsDefined(data.Half.Value)) return new ServiceResult(ServiceOutcome.Invalid, "Angiv, hvilken halvdel dagen hører til.");

            return null;
        }

        private static CampDayDto ToDto(CampDay day)
        {
            return new CampDayDto
            {
                Id = day.Id,
                Date = day.Date,
                Half = day.Half
            };
        }
    }
}

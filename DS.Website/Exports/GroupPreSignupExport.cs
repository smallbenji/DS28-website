using ClosedXML.Excel;
using DS.Models;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Exports
{
    public class GroupPreSignupExport(DataDbContext dataDb) : DataExport
    {
        public override string Key => "group-pre-signup";
        public override string Title => "Grupper og forhåndstilmelding";
        public override string Description => "Alle grupper med distrikt og de anmeldte antal pr. aldersgruppe. Grupper uden forhåndstilmelding står med tomme felter.";
        public override string FileName => "grupper-og-forhandstilmelding";
        public override string RequiredRole => nameof(AppRoles.GroupsView);

        public override async Task BuildAsync(IXLWorkbook workbook, CancellationToken cancellationToken)
        {
            var groups = await dataDb.Groups
                .AsNoTracking()
                .Include(g => g.PreSignup)
                .OrderBy(g => g.District)
                .ThenBy(g => g.Name)
                .ToListAsync(cancellationToken);

            var sheet = workbook.AddExportSheet(
                "Grupper",
                "Gruppe-ID",
                "Gruppenavn",
                "Distrikt",
                "Beaver",
                "Wolf",
                "Junior",
                "Trop",
                "Senior",
                "Rover",
                "Leder",
                "I alt");

            foreach (var group in groups)
            {
                var signup = group.PreSignup;

                sheet.AddExportRow(
                    group.Id,
                    group.Name,
                    Labels.For(group.District),
                    signup?.Beaver,
                    signup?.Wolf,
                    signup?.Junior,
                    signup?.Trop,
                    signup?.Senior,
                    signup?.Rover,
                    signup?.Leader,
                    Sum(signup));
            }

            sheet.FitColumns(10, 40);
        }

        private static int? Sum(GroupPreSignup signup)
        {
            if (signup == null) return null;

            return signup.Beaver + signup.Wolf + signup.Junior + signup.Trop
                + signup.Senior + signup.Rover + signup.Leader;
        }
    }
}

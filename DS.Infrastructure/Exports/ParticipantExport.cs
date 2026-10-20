using ClosedXML.Excel;
using DS.Domain;
using DS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DS.Infrastructure.Exports
{
    public class ParticipantExport(DataDbContext dataDb) : DataExport
    {
        public override string Key => "participant-export";

        public override string Title => "Deltager";

        public override string Description => "Alle deltagere";

        public override string FileName => "deltager";

        public override string RequiredRole => nameof(AppRoles.GroupsView);

        public override async Task BuildAsync(IXLWorkbook workbook, CancellationToken cancellationToken)
        {
            var participants = await dataDb.Scouts
                .AsNoTracking()
                .Where(s => s.Group.DeletedAt == null)
                .OrderBy(s => s.Name)
                .ThenBy(s => s.Group.Name)
                .Select(s => new { s.Name, GroupName = s.Group.Name })
                .ToListAsync(cancellationToken);
            
            var sheet = workbook.AddExportSheet(
                "Navn",
                "Gruppe"
            );

            foreach (var participant in participants)
            {
                sheet.AddExportRow(
                    participant.Name,
                    participant.GroupName
                );

                sheet.FitColumns(2, 40);
            }
        }
    }
}
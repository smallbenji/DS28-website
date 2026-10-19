using ClosedXML.Excel;
using DS.Data;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Exports
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
                .Include(s => s.Group)
                .OrderBy(s => s.Name)
                .ThenBy(s => s.Group.Name)
                .ToListAsync(cancellationToken);
            
            var sheet = workbook.AddExportSheet(
                "Navn",
                "Gruppe"
            );

            foreach (var participant in participants)
            {
                sheet.AddExportRow(
                    participant.Name,
                    participant.Group.Name
                );

                sheet.FitColumns(2, 40);
            }
        }
    }
}
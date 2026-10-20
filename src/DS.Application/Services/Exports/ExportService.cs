using DS.Domain.DTOs;

namespace DS.Application.Services.Exports
{
    public class ExportService(IEnumerable<IDataExport> exports)
    {
        public List<DataExportDto> GetExports(IReadOnlyCollection<string> roles)
        {
            return exports
                .Where(export => roles.Contains(export.RequiredRole))
                .Select(export => new DataExportDto
                {
                    Key = export.Key,
                    Title = export.Title,
                    Description = export.Description,
                    FileName = export.FileName,
                    RequiredRole = export.RequiredRole
                })
                .ToList();
        }

        public async Task<ExportResult> Download(string key, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
        {
            var export = exports.FirstOrDefault(export => export.Key == key);
            if (export == null) return new ExportResult(ExportOutcome.NotFound);

            if (!roles.Contains(export.RequiredRole)) return new ExportResult(ExportOutcome.Forbidden);

            var content = await export.BuildFileAsync(cancellationToken);

            return new ExportResult(ExportOutcome.Success, content, $"{export.FileName}.xlsx");
        }
    }
}

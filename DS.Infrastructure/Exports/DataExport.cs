using DS.Application.Services.Exports;
using ClosedXML.Excel;

namespace DS.Infrastructure.Exports
{
    public abstract class DataExport : IDataExport
    {
        public abstract string Key { get; }
        public abstract string Title { get; }
        public abstract string Description { get; }
        public abstract string FileName { get; }

        public abstract string RequiredRole { get; }

        public async Task<Stream> BuildFileAsync(CancellationToken cancellationToken)
        {
            using var workbook = new XLWorkbook();
            await BuildAsync(workbook, cancellationToken);

            var stream = new MemoryStream();
            try
            {
                workbook.SaveAs(stream);
                stream.Position = 0;

                return stream;
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        public abstract Task BuildAsync(IXLWorkbook workbook, CancellationToken cancellationToken);
    }
}

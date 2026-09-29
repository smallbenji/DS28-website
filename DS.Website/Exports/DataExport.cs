using ClosedXML.Excel;

namespace DS.Website.Exports
{
    public abstract class DataExport
    {
        public abstract string Key { get; }
        public abstract string Title { get; }
        public abstract string Description { get; }
        public abstract string FileName { get; }

        // Ud over ExportsView. Checkes i både listen og ved download, så nøglen ikke kan gættes.
        public abstract string RequiredRole { get; }

        public abstract Task BuildAsync(IXLWorkbook workbook, CancellationToken cancellationToken);
    }
}

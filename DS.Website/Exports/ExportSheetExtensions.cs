using ClosedXML.Excel;

namespace DS.Website.Exports
{
    public static class ExportSheetExtensions
    {
        private const string HeaderFill = "#F2F2F2";

        public static IXLWorksheet AddExportSheet(this IXLWorkbook workbook, string name, params string[] headers)
        {
            var sheet = workbook.AddWorksheet(name);

            for (var i = 0; i < headers.Length; i++)
            {
                Set(sheet.Cell(1, i + 1), headers[i]);
            }

            var header = sheet.Row(1);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderFill);
            header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            sheet.SheetView.FreezeRows(1);
            sheet.Range(1, 1, 1, headers.Length).SetAutoFilter();

            return sheet;
        }

        public static IXLRow AddExportRow(this IXLWorksheet sheet, params object[] values)
        {
            var row = sheet.Row(sheet.LastRowUsed()?.RowNumber() + 1 ?? 2);

            for (var i = 0; i < values.Length; i++)
            {
                Set(row.Cell(i + 1), values[i]);
            }

            return row;
        }

        public static void SetNumberFormat(this IXLWorksheet sheet, int column, string format)
        {
            sheet.Column(column).Style.NumberFormat.Format = format;
        }

        public static void FitColumns(this IXLWorksheet sheet, double minWidth, double maxWidth)
        {
            sheet.Columns().AdjustToContents(minWidth, maxWidth);
        }

        private static void Set(IXLCell cell, object value)
        {
            switch (value)
            {
                case null:
                    cell.Clear();
                    break;
                case string text:
                    cell.Value = text;
                    break;
                case int number:
                    cell.Value = number;
                    break;
                case double number:
                    cell.Value = number;
                    break;
                case bool flag:
                    cell.Value = flag;
                    break;
                case DateTime timestamp:
                    cell.Value = timestamp;
                    break;
                default:
                    cell.Value = value.ToString();
                    break;
            }
        }
    }
}

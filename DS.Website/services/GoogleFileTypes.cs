namespace DS.Website.Services
{
    public static class GoogleFileTypes
    {
        public const string DocumentKind = "doc";
        public const string SpreadsheetKind = "sheet";
        public const string PresentationKind = "slides";
        public const string OtherKind = "file";

        public const string FolderMimeType = "application/vnd.google-apps.folder";

        private const string DocumentMimeType = "application/vnd.google-apps.document";
        private const string SpreadsheetMimeType = "application/vnd.google-apps.spreadsheet";
        private const string PresentationMimeType = "application/vnd.google-apps.presentation";

        public static bool IsSupportedKind(string kind)
        {
            return kind == DocumentKind || kind == SpreadsheetKind || kind == PresentationKind;
        }

        public static string MimeTypeForKind(string kind)
        {
            return kind switch
            {
                DocumentKind => DocumentMimeType,
                SpreadsheetKind => SpreadsheetMimeType,
                PresentationKind => PresentationMimeType,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Ukendt filtype."),
            };
        }

        public static string KindForMimeType(string mimeType)
        {
            return mimeType switch
            {
                DocumentMimeType => DocumentKind,
                SpreadsheetMimeType => SpreadsheetKind,
                PresentationMimeType => PresentationKind,
                _ => OtherKind,
            };
        }

        public static string OpenUrl(string mimeType, string fileId)
        {
            var editorPath = EditorPathForMimeType(mimeType);

            return editorPath == null
                ? $"https://drive.google.com/file/d/{fileId}/view"
                : $"https://docs.google.com/{editorPath}/d/{fileId}";
        }

        private static string EditorPathForMimeType(string mimeType)
        {
            return mimeType switch
            {
                DocumentMimeType => "document",
                SpreadsheetMimeType => "spreadsheets",
                PresentationMimeType => "presentation",
                _ => null,
            };
        }
    }
}

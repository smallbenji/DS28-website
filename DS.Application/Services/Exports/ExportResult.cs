namespace DS.Application.Services.Exports
{
    public enum ExportOutcome
    {
        Success,
        NotFound,
        Forbidden
    }

    public record ExportResult(ExportOutcome Outcome, Stream Content = null, string FileName = null);
}

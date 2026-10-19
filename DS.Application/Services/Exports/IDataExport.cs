namespace DS.Application.Services.Exports
{
    public interface IDataExport
    {
        string Key { get; }
        string Title { get; }
        string Description { get; }
        string FileName { get; }
        string RequiredRole { get; }

        Task<Stream> BuildFileAsync(CancellationToken cancellationToken);
    }
}

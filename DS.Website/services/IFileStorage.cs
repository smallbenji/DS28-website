namespace DS.Website.Services
{
    public interface IFileStorage
    {
        Task WriteAsync(string key, Stream content, CancellationToken cancellationToken = default);
        Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
        Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    }
}

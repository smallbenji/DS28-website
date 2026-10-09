using DS;
using Microsoft.Extensions.Options;

namespace DS.Website.Services
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string root;

        public LocalFileStorage(IOptions<DSSettings> options)
        {
            var configuredPath = options.Value.FileStoragePath;
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                throw new InvalidOperationException("DS:FileStoragePath er ikke konfigureret.");
            }

            root = Path.GetFullPath(configuredPath);
            Directory.CreateDirectory(root);
        }

        public async Task WriteAsync(string key, Stream content, CancellationToken cancellationToken = default)
        {
            var path = Resolve(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            await using var target = File.Create(path);
            await content.CopyToAsync(target, cancellationToken);
        }

        public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
        {
            Stream stream = File.OpenRead(Resolve(key));

            return Task.FromResult(stream);
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(File.Exists(Resolve(key)));
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            var path = Resolve(key);
            if (File.Exists(path)) File.Delete(path);

            return Task.CompletedTask;
        }

        private string Resolve(string key)
        {
            // Sikkerhed: nøglen må ikke kunne pege uden for lagerroden.
            var path = Path.GetFullPath(Path.Combine(root, key));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Ugyldig lagerstienøgle: {key}");
            }

            return path;
        }
    }
}

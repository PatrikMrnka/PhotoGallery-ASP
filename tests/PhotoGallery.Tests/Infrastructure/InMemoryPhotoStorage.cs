using System.Collections.Concurrent;
using PhotoGallery.Web.Services;

namespace PhotoGallery.Tests.Infrastructure;

internal sealed class InMemoryPhotoStorage : IPhotoStorage
{
    public ConcurrentDictionary<Guid, byte[]> Files { get; } = new();

    public Task SaveAsync(Guid photoId, byte[] data, CancellationToken cancellationToken = default)
    {
        Files[photoId] = data;
        return Task.CompletedTask;
    }

    public Stream? OpenRead(Guid photoId) =>
        Files.TryGetValue(photoId, out var data) ? new MemoryStream(data, writable: false) : null;

    public void Delete(Guid photoId) => Files.TryRemove(photoId, out _);

    public IEnumerable<Guid> ListIds() => Files.Keys.ToList();

    public void DeleteAll() => Files.Clear();
}

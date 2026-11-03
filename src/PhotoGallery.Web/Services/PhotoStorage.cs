namespace PhotoGallery.Web.Services;

/// <summary>Stores the original image files. Database rows only reference them by photo id.</summary>
public interface IPhotoStorage
{
    Task SaveAsync(Guid photoId, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>Opens the stored file for reading, or returns <c>null</c> when it does not exist.</summary>
    Stream? OpenRead(Guid photoId);

    void Delete(Guid photoId);

    /// <summary>Ids of all stored files. Used to clean up files whose database row no longer exists.</summary>
    IEnumerable<Guid> ListIds();

    void DeleteAll();
}

public sealed class FileSystemPhotoStorage : IPhotoStorage
{
    private readonly string _root;

    public FileSystemPhotoStorage(string rootPath)
    {
        _root = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_root);
    }

    // File names are always generated from a Guid, so user input can never reach the file system path.
    private string PathFor(Guid photoId) => Path.Combine(_root, photoId.ToString("N"));

    public async Task SaveAsync(Guid photoId, byte[] data, CancellationToken cancellationToken = default)
    {
        await File.WriteAllBytesAsync(PathFor(photoId), data, cancellationToken);
    }

    public Stream? OpenRead(Guid photoId)
    {
        var path = PathFor(photoId);
        return File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true)
            : null;
    }

    public void Delete(Guid photoId)
    {
        var path = PathFor(photoId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public IEnumerable<Guid> ListIds()
    {
        foreach (var file in Directory.EnumerateFiles(_root))
        {
            if (Guid.TryParseExact(Path.GetFileName(file), "N", out var id))
            {
                yield return id;
            }
        }
    }

    public void DeleteAll()
    {
        foreach (var id in ListIds().ToList())
        {
            Delete(id);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoGallery.Web.Data;
using PhotoGallery.Web.Models;
using PhotoGallery.Web.Options;

namespace PhotoGallery.Web.Services;

/// <summary>
/// All gallery use cases. Razor Pages only translate HTTP to calls on this service,
/// so every permission check lives here (see <see cref="GalleryAccess"/>).
/// </summary>
public sealed class GalleryService(
    ApplicationDbContext db,
    IPhotoStorage storage,
    IImageProcessor imageProcessor,
    IOptions<UploadOptions> uploadOptions,
    TimeProvider timeProvider,
    ILogger<GalleryService> logger)
{
    public const string DefaultAlbumName = "All photos";

    private readonly UploadOptions _upload = uploadOptions.Value;

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    // ---------------------------------------------------------------- queries

    public async Task<GalleryStats> GetStatsAsync(CancellationToken ct = default) =>
        new(
            await db.Photos.CountAsync(p => p.IsPublic, ct),
            await db.Albums.CountAsync(a => a.IsPublic, ct));

    public async Task<PagedResult<PhotoSummary>> GetPublicPhotosAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Photos.AsNoTracking().Where(p => p.IsPublic);
        var total = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await ProjectPhotos(query
                .OrderByDescending(p => p.UploadedAt)
                .ThenBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize))
            .ToListAsync(ct);

        return new PagedResult<PhotoSummary>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<AlbumSummary>> GetPublicAlbumsAsync(string? viewerId, int take, CancellationToken ct = default)
    {
        var query = db.Albums.AsNoTracking()
            .Where(a => a.IsPublic)
            .OrderByDescending(a => a.CreatedAt)
            .Take(take);

        return await ProjectAlbums(query, viewerId).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AlbumSummary>> GetUserAlbumsAsync(string userId, SortOrder sort, CancellationToken ct = default)
    {
        await GetOrCreateDefaultAlbumAsync(userId, ct);

        var query = db.Albums.AsNoTracking()
            .Where(a => a.OwnerId == userId)
            .OrderByDescending(a => a.IsDefault);

        query = sort switch
        {
            SortOrder.Name => query.ThenBy(a => a.Name),
            SortOrder.Oldest => query.ThenBy(a => a.CreatedAt),
            _ => query.ThenByDescending(a => a.CreatedAt),
        };

        return await ProjectAlbums(query, userId).ToListAsync(ct);
    }

    /// <summary>Returns the album with the photos the viewer may see, or <c>null</c> when it does not exist or is hidden.</summary>
    public async Task<AlbumDetails?> GetAlbumAsync(Guid albumId, string? viewerId, SortOrder sort, CancellationToken ct = default)
    {
        var album = await ProjectAlbums(
                db.Albums.AsNoTracking()
                    .Where(a => a.Id == albumId)
                    .Where(GalleryAccess.AlbumVisibleTo(viewerId)),
                viewerId)
            .FirstOrDefaultAsync(ct);

        if (album is null)
        {
            return null;
        }

        var links = db.AlbumPhotos.AsNoTracking()
            .Where(ap => ap.AlbumId == albumId)
            .Where(ap => ap.Photo!.IsPublic || (viewerId != null && ap.Photo.OwnerId == viewerId));

        links = sort switch
        {
            SortOrder.Name => links.OrderBy(ap => ap.Photo!.OriginalName),
            SortOrder.Oldest => links.OrderBy(ap => ap.Photo!.UploadedAt),
            _ => links.OrderByDescending(ap => ap.Photo!.UploadedAt),
        };

        var photos = await links
            .Select(ap => new PhotoSummary(
                ap.PhotoId,
                ap.Photo!.OriginalName,
                ap.Photo.IsPublic,
                ap.Photo.UploadedAt,
                UserDisplayName.From(ap.Photo.Owner!.UserName)))
            .ToListAsync(ct);

        return new AlbumDetails(album, GalleryAccess.IsOwner(album.OwnerId, viewerId), photos);
    }

    /// <summary>Returns the photo, or <c>null</c> when it does not exist or the viewer may not see it.</summary>
    public async Task<PhotoDetails?> GetPhotoAsync(Guid photoId, string? viewerId, CancellationToken ct = default)
    {
        var photo = await db.Photos.AsNoTracking()
            .Where(p => p.Id == photoId)
            .Where(GalleryAccess.PhotoVisibleTo(viewerId))
            .Select(p => new
            {
                p.Id,
                p.OriginalName,
                p.ContentType,
                p.SizeBytes,
                p.Width,
                p.Height,
                p.UploadedAt,
                p.IsPublic,
                p.OwnerId,
                OwnerUserName = p.Owner!.UserName,
            })
            .FirstOrDefaultAsync(ct);

        if (photo is null)
        {
            return null;
        }

        IReadOnlyList<AlbumRef> viewerAlbums = viewerId is null
            ? []
            : await db.AlbumPhotos.AsNoTracking()
                .Where(ap => ap.PhotoId == photoId && ap.Album!.OwnerId == viewerId)
                .OrderByDescending(ap => ap.Album!.IsDefault)
                .ThenBy(ap => ap.Album!.Name)
                .Select(ap => new AlbumRef(ap.AlbumId, ap.Album!.Name, ap.Album.IsDefault))
                .ToListAsync(ct);

        return new PhotoDetails(
            photo.Id,
            photo.OriginalName,
            photo.ContentType,
            photo.SizeBytes,
            photo.Width,
            photo.Height,
            photo.UploadedAt,
            photo.IsPublic,
            UserDisplayName.From(photo.OwnerUserName),
            GalleryAccess.IsOwner(photo.OwnerId, viewerId),
            viewerAlbums);
    }

    /// <summary>The user's own albums the photo can still be added to (the default album is managed automatically).</summary>
    public async Task<IReadOnlyList<AlbumRef>> GetAlbumsForAddingAsync(Guid photoId, string userId, CancellationToken ct = default) =>
        await db.Albums.AsNoTracking()
            .Where(a => a.OwnerId == userId && !a.IsDefault && !a.AlbumPhotos.Any(ap => ap.PhotoId == photoId))
            .OrderBy(a => a.Name)
            .Select(a => new AlbumRef(a.Id, a.Name, a.IsDefault))
            .ToListAsync(ct);

    /// <summary>The user's own albums that can be chosen as an upload target.</summary>
    public async Task<IReadOnlyList<AlbumSummary>> GetUploadTargetsAsync(string userId, CancellationToken ct = default) =>
        await ProjectAlbums(
                db.Albums.AsNoTracking()
                    .Where(a => a.OwnerId == userId && !a.IsDefault)
                    .OrderBy(a => a.Name),
                userId)
            .ToListAsync(ct);

    public async Task<OriginalImage?> OpenOriginalAsync(Guid photoId, string? viewerId, CancellationToken ct = default)
    {
        var photo = await db.Photos.AsNoTracking()
            .Where(p => p.Id == photoId)
            .Where(GalleryAccess.PhotoVisibleTo(viewerId))
            .Select(p => new { p.ContentType, p.OriginalName, p.IsPublic })
            .FirstOrDefaultAsync(ct);

        if (photo is null)
        {
            return null;
        }

        var stream = storage.OpenRead(photoId);
        if (stream is null)
        {
            logger.LogWarning("File for photo {PhotoId} is missing in storage", photoId);
            return null;
        }

        return new OriginalImage(stream, photo.ContentType, photo.OriginalName, photo.IsPublic);
    }

    public Task<ThumbnailImage?> GetThumbnailAsync(Guid photoId, string? viewerId, CancellationToken ct = default) =>
        db.Thumbnails.AsNoTracking()
            .Where(t => t.PhotoId == photoId)
            .Where(t => t.Photo!.IsPublic || (viewerId != null && t.Photo.OwnerId == viewerId))
            .Select(t => new ThumbnailImage(t.Data, t.ContentType, t.Photo!.IsPublic))
            .FirstOrDefaultAsync(ct);

    // --------------------------------------------------------------- commands

    public async Task<UploadResult> UploadAsync(
        string userId,
        IReadOnlyList<UploadFile> files,
        bool isPublic,
        Guid? albumId,
        CancellationToken ct = default)
    {
        var errors = new List<string>();
        var uploaded = new List<Guid>();

        if (files.Count == 0)
        {
            return new UploadResult(uploaded, ["Select at least one image."]);
        }

        if (files.Count > _upload.MaxFilesPerRequest)
        {
            return new UploadResult(uploaded, [$"You can upload at most {_upload.MaxFilesPerRequest} files at once."]);
        }

        Album? target = null;
        if (albumId is not null)
        {
            target = await db.Albums.FirstOrDefaultAsync(a => a.Id == albumId && a.OwnerId == userId, ct);
            if (target is null)
            {
                return new UploadResult(uploaded, ["The selected album does not exist."]);
            }
        }

        var defaultAlbum = await GetOrCreateDefaultAlbumAsync(userId, ct);

        var remaining = int.MaxValue;
        if (_upload.MaxPhotosPerUser > 0)
        {
            var owned = await db.Photos.CountAsync(p => p.OwnerId == userId, ct);
            remaining = Math.Max(0, _upload.MaxPhotosPerUser - owned);
        }

        foreach (var file in files)
        {
            var name = SanitizeFileName(file.FileName);

            if (remaining == 0)
            {
                errors.Add($"{name}: you have reached the limit of {_upload.MaxPhotosPerUser} photos.");
                continue;
            }

            if (file.Length == 0)
            {
                errors.Add($"{name}: the file is empty.");
                continue;
            }

            if (file.Length > _upload.MaxFileBytes)
            {
                errors.Add($"{name}: the file is larger than {_upload.MaxFileBytes / (1024 * 1024)} MB.");
                continue;
            }

            ProcessedImage image;
            try
            {
                image = await imageProcessor.ProcessAsync(file.Content, ct);
            }
            catch (InvalidImageException ex)
            {
                errors.Add($"{name}: {ex.Message}");
                continue;
            }

            var now = UtcNow;
            var photo = new Photo
            {
                Id = Guid.NewGuid(),
                OriginalName = name,
                ContentType = image.ContentType,
                SizeBytes = image.Data.LongLength,
                Width = image.Width,
                Height = image.Height,
                UploadedAt = now,
                IsPublic = isPublic,
                OwnerId = userId,
                Thumbnail = new PhotoThumbnail { Data = image.Thumbnail, ContentType = image.ThumbnailContentType },
            };

            photo.AlbumPhotos.Add(new AlbumPhoto { AlbumId = defaultAlbum.Id, AddedAt = now });
            if (target is not null && target.Id != defaultAlbum.Id)
            {
                photo.AlbumPhotos.Add(new AlbumPhoto { AlbumId = target.Id, AddedAt = now });
            }

            await storage.SaveAsync(photo.Id, image.Data, ct);
            db.Photos.Add(photo);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch
            {
                storage.Delete(photo.Id);
                throw;
            }

            uploaded.Add(photo.Id);
            remaining--;
        }

        return new UploadResult(uploaded, errors);
    }

    public async Task<OperationResult> SetPhotoVisibilityAsync(Guid photoId, string userId, bool isPublic, CancellationToken ct = default)
    {
        var photo = await db.Photos.FirstOrDefaultAsync(p => p.Id == photoId, ct);
        if (photo is null || !GalleryAccess.CanView(photo, userId))
        {
            return OperationResult.NotFound();
        }

        if (!GalleryAccess.IsOwner(photo.OwnerId, userId))
        {
            return OperationResult.Forbidden("Only the owner can change who sees this photo.");
        }

        photo.IsPublic = isPublic;
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok(isPublic ? "The photo is now public." : "The photo is now private.");
    }

    public async Task<OperationResult> DeletePhotoAsync(Guid photoId, string userId, CancellationToken ct = default)
    {
        var photo = await db.Photos.FirstOrDefaultAsync(p => p.Id == photoId, ct);
        if (photo is null || !GalleryAccess.CanView(photo, userId))
        {
            return OperationResult.NotFound();
        }

        if (!GalleryAccess.IsOwner(photo.OwnerId, userId))
        {
            return OperationResult.Forbidden("Only the owner can delete this photo.");
        }

        db.Photos.Remove(photo);
        await db.SaveChangesAsync(ct);
        storage.Delete(photoId);
        return OperationResult.Ok($"\"{photo.OriginalName}\" was deleted.");
    }

    public async Task<(OperationResult Result, Guid? AlbumId)> CreateAlbumAsync(string userId, string? name, CancellationToken ct = default)
    {
        if (ValidateAlbumName(name) is { } error)
        {
            return (OperationResult.Invalid(error), null);
        }

        var album = new Album
        {
            Id = Guid.NewGuid(),
            Name = name!.Trim(),
            OwnerId = userId,
            CreatedAt = UtcNow,
        };

        db.Albums.Add(album);
        await db.SaveChangesAsync(ct);
        return (OperationResult.Ok($"Album \"{album.Name}\" was created."), album.Id);
    }

    public async Task<OperationResult> RenameAlbumAsync(Guid albumId, string userId, string? name, CancellationToken ct = default)
    {
        var (album, failure) = await FindOwnAlbumForChangeAsync(albumId, userId, "renamed", ct);
        if (failure is not null)
        {
            return failure;
        }

        if (ValidateAlbumName(name) is { } error)
        {
            return OperationResult.Invalid(error);
        }

        album!.Name = name!.Trim();
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok("The album was renamed.");
    }

    /// <summary>
    /// Publishing an album also publishes the owner's photos in it, otherwise visitors would see an empty album.
    /// Making it private only hides the album; photos keep their own visibility.
    /// </summary>
    public async Task<OperationResult> SetAlbumVisibilityAsync(Guid albumId, string userId, bool isPublic, CancellationToken ct = default)
    {
        var (album, failure) = await FindOwnAlbumForChangeAsync(albumId, userId, "made public", ct);
        if (failure is not null)
        {
            return failure;
        }

        album!.IsPublic = isPublic;

        if (!isPublic)
        {
            await db.SaveChangesAsync(ct);
            return OperationResult.Ok("The album is now private. Photos in it keep their own visibility.");
        }

        var privatePhotos = await db.Photos
            .Where(p => p.OwnerId == userId && !p.IsPublic && p.AlbumPhotos.Any(ap => ap.AlbumId == albumId))
            .ToListAsync(ct);

        foreach (var photo in privatePhotos)
        {
            photo.IsPublic = true;
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok(privatePhotos.Count == 0
            ? "The album is now public."
            : $"The album is now public, together with {privatePhotos.Count} of your photos in it.");
    }

    public async Task<OperationResult> DeleteAlbumAsync(Guid albumId, string userId, CancellationToken ct = default)
    {
        var (album, failure) = await FindOwnAlbumForChangeAsync(albumId, userId, "deleted", ct);
        if (failure is not null)
        {
            return failure;
        }

        db.Albums.Remove(album!);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok($"Album \"{album!.Name}\" was deleted. Its photos are still in {DefaultAlbumName}.");
    }

    public async Task<OperationResult> AddPhotoToAlbumAsync(Guid photoId, Guid albumId, string userId, CancellationToken ct = default)
    {
        var album = await db.Albums.FirstOrDefaultAsync(a => a.Id == albumId && a.OwnerId == userId, ct);
        if (album is null)
        {
            return OperationResult.NotFound("The album was not found.");
        }

        if (album.IsDefault)
        {
            return OperationResult.Invalid($"{DefaultAlbumName} contains your uploads automatically.");
        }

        var photo = await db.Photos.FirstOrDefaultAsync(p => p.Id == photoId, ct);
        if (photo is null || !GalleryAccess.CanView(photo, userId))
        {
            return OperationResult.NotFound("The photo was not found.");
        }

        if (await db.AlbumPhotos.AnyAsync(ap => ap.AlbumId == albumId && ap.PhotoId == photoId, ct))
        {
            return OperationResult.Invalid($"The photo is already in \"{album.Name}\".");
        }

        db.AlbumPhotos.Add(new AlbumPhoto { AlbumId = albumId, PhotoId = photoId, AddedAt = UtcNow });
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok($"The photo was added to \"{album.Name}\".");
    }

    public async Task<OperationResult> RemovePhotoFromAlbumAsync(Guid photoId, Guid albumId, string userId, CancellationToken ct = default)
    {
        var album = await db.Albums.FirstOrDefaultAsync(a => a.Id == albumId && a.OwnerId == userId, ct);
        if (album is null)
        {
            return OperationResult.NotFound("The album was not found.");
        }

        if (album.IsDefault)
        {
            return OperationResult.Invalid($"Photos cannot be removed from {DefaultAlbumName}. Delete the photo instead.");
        }

        var link = await db.AlbumPhotos.FirstOrDefaultAsync(ap => ap.AlbumId == albumId && ap.PhotoId == photoId, ct);
        if (link is null)
        {
            return OperationResult.NotFound("The photo is not in this album.");
        }

        db.AlbumPhotos.Remove(link);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok($"The photo was removed from \"{album.Name}\".");
    }

    /// <summary>Every user gets a default album lazily, the first time they need one.</summary>
    public async Task<Album> GetOrCreateDefaultAlbumAsync(string userId, CancellationToken ct = default)
    {
        var album = await db.Albums.FirstOrDefaultAsync(a => a.OwnerId == userId && a.IsDefault, ct);
        if (album is not null)
        {
            return album;
        }

        album = new Album
        {
            Id = Guid.NewGuid(),
            Name = DefaultAlbumName,
            IsDefault = true,
            OwnerId = userId,
            CreatedAt = UtcNow,
        };

        db.Albums.Add(album);
        try
        {
            await db.SaveChangesAsync(ct);
            return album;
        }
        catch (DbUpdateException)
        {
            // Another request created it at the same moment (guarded by a unique index).
            db.Entry(album).State = EntityState.Detached;
            return await db.Albums.FirstAsync(a => a.OwnerId == userId && a.IsDefault, ct);
        }
    }

    /// <summary>Removes stored files that no longer have a database row, e.g. after an account was deleted.</summary>
    public async Task<int> DeleteOrphanedFilesAsync(CancellationToken ct = default)
    {
        var known = (await db.Photos.Select(p => p.Id).ToListAsync(ct)).ToHashSet();
        var orphans = storage.ListIds().Where(id => !known.Contains(id)).ToList();
        foreach (var id in orphans)
        {
            storage.Delete(id);
        }

        return orphans.Count;
    }

    // ---------------------------------------------------------------- helpers

    private async Task<(Album? Album, OperationResult? Failure)> FindOwnAlbumForChangeAsync(
        Guid albumId, string userId, string action, CancellationToken ct)
    {
        var album = await db.Albums.FirstOrDefaultAsync(a => a.Id == albumId, ct);
        if (album is null || !GalleryAccess.CanView(album, userId))
        {
            return (null, OperationResult.NotFound("The album was not found."));
        }

        if (!GalleryAccess.IsOwner(album.OwnerId, userId))
        {
            return (null, OperationResult.Forbidden("Only the owner can change this album."));
        }

        if (album.IsDefault)
        {
            return (null, OperationResult.Invalid($"{DefaultAlbumName} cannot be {action}."));
        }

        return (album, null);
    }

    private static string? ValidateAlbumName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Enter an album name.";
        }

        if (name.Trim().Length > Album.NameMaxLength)
        {
            return $"The album name can have at most {Album.NameMaxLength} characters.";
        }

        return null;
    }

    internal static string SanitizeFileName(string? fileName)
    {
        // Browsers may send a full client path; keep only the last segment for both / and \ separators.
        var name = fileName ?? string.Empty;
        name = name[(name.LastIndexOfAny(['/', '\\']) + 1)..];
        name = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (name.Length == 0)
        {
            return "photo";
        }

        return name.Length <= 255 ? name : name[..255];
    }

    private static IQueryable<PhotoSummary> ProjectPhotos(IQueryable<Photo> photos) =>
        photos.Select(p => new PhotoSummary(
            p.Id,
            p.OriginalName,
            p.IsPublic,
            p.UploadedAt,
            UserDisplayName.From(p.Owner!.UserName)));

    private static IQueryable<AlbumSummary> ProjectAlbums(IQueryable<Album> albums, string? viewerId) =>
        albums.Select(a => new AlbumSummary(
            a.Id,
            a.Name,
            a.IsPublic,
            a.IsDefault,
            a.CreatedAt,
            a.OwnerId,
            UserDisplayName.From(a.Owner!.UserName),
            a.AlbumPhotos.Count(ap => ap.Photo!.IsPublic || (viewerId != null && ap.Photo.OwnerId == viewerId)),
            a.AlbumPhotos
                .Where(ap => ap.Photo!.IsPublic || (viewerId != null && ap.Photo.OwnerId == viewerId))
                .OrderByDescending(ap => ap.Photo!.UploadedAt)
                .Select(ap => (Guid?)ap.PhotoId)
                .FirstOrDefault()));
}

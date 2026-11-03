namespace PhotoGallery.Web.Services;

public enum OperationStatus
{
    Ok,
    NotFound,
    Forbidden,
    Invalid,
}

/// <summary>Outcome of a command. <see cref="Message"/> is meant to be shown to the user.</summary>
public sealed record OperationResult(OperationStatus Status, string Message)
{
    public bool Succeeded => Status == OperationStatus.Ok;

    public static OperationResult Ok(string message) => new(OperationStatus.Ok, message);

    public static OperationResult NotFound(string message = "The item was not found.") =>
        new(OperationStatus.NotFound, message);

    public static OperationResult Forbidden(string message) => new(OperationStatus.Forbidden, message);

    public static OperationResult Invalid(string message) => new(OperationStatus.Invalid, message);
}

public enum SortOrder
{
    Newest,
    Oldest,
    Name,
}

public sealed record PhotoSummary(Guid Id, string Name, bool IsPublic, DateTime UploadedAt, string OwnerName);

public sealed record AlbumSummary(
    Guid Id,
    string Name,
    bool IsPublic,
    bool IsDefault,
    DateTime CreatedAt,
    string OwnerId,
    string OwnerName,
    int PhotoCount,
    Guid? CoverPhotoId);

public sealed record AlbumRef(Guid Id, string Name, bool IsDefault);

public sealed record AlbumDetails(AlbumSummary Album, bool IsOwner, IReadOnlyList<PhotoSummary> Photos);

public sealed record PhotoDetails(
    Guid Id,
    string Name,
    string ContentType,
    long SizeBytes,
    int Width,
    int Height,
    DateTime UploadedAt,
    bool IsPublic,
    string OwnerName,
    bool IsOwner,
    IReadOnlyList<AlbumRef> ViewerAlbums);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}

public sealed record GalleryStats(int PublicPhotos, int PublicAlbums);

/// <summary>An uploaded file, decoupled from ASP.NET's <c>IFormFile</c> so the service is easy to test.</summary>
public sealed record UploadFile(string FileName, long Length, Stream Content);

public sealed record UploadResult(IReadOnlyList<Guid> PhotoIds, IReadOnlyList<string> Errors);

public sealed record OriginalImage(Stream Content, string ContentType, string FileName, bool IsPublic);

public sealed record ThumbnailImage(byte[] Data, string ContentType, bool IsPublic);

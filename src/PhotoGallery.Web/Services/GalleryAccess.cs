using System.Linq.Expressions;
using PhotoGallery.Web.Models;

namespace PhotoGallery.Web.Services;

/// <summary>
/// The single place that defines who may see what.
/// <list type="bullet">
/// <item>A photo is visible to its owner, or to everyone when it is public.</item>
/// <item>An album is visible to its owner, or to everyone when it is public.
/// Inside an album, each photo is still filtered by its own visibility.</item>
/// <item>Only the owner can change or delete a photo or an album.</item>
/// </list>
/// </summary>
public static class GalleryAccess
{
    public static bool IsOwner(string ownerId, string? userId) =>
        userId is not null && string.Equals(ownerId, userId, StringComparison.Ordinal);

    public static bool CanView(Photo photo, string? userId) => photo.IsPublic || IsOwner(photo.OwnerId, userId);

    public static bool CanView(Album album, string? userId) => album.IsPublic || IsOwner(album.OwnerId, userId);

    /// <summary>Same rule as <see cref="CanView(Photo, string?)"/>, translatable to SQL.</summary>
    public static Expression<Func<Photo, bool>> PhotoVisibleTo(string? userId) =>
        p => p.IsPublic || (userId != null && p.OwnerId == userId);

    /// <summary>Same rule as <see cref="CanView(Album, string?)"/>, translatable to SQL.</summary>
    public static Expression<Func<Album, bool>> AlbumVisibleTo(string? userId) =>
        a => a.IsPublic || (userId != null && a.OwnerId == userId);
}

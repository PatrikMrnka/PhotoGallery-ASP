using Microsoft.AspNetCore.Mvc;
using PhotoGallery.Web.Services;

namespace PhotoGallery.Web.Pages.Photos;

public class DetailsModel(GalleryService gallery) : GalleryPageModel
{
    public PhotoDetails Photo { get; private set; } = default!;

    /// <summary>The user's albums the photo can still be added to.</summary>
    public IReadOnlyList<AlbumRef> AddableAlbums { get; private set; } = [];

    /// <summary>Album the visitor came from, used for the back link and "remove from album".</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? AlbumId { get; set; }

    /// <summary>The context album, if it belongs to the current user.</summary>
    public AlbumRef? OwnContextAlbum => Photo.ViewerAlbums.FirstOrDefault(a => a.Id == AlbumId);

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        var photo = await gallery.GetPhotoAsync(id, CurrentUserId, ct);
        if (photo is null)
        {
            return NotFound();
        }

        Photo = photo;
        if (CurrentUserId is { } userId)
        {
            AddableAlbums = await gallery.GetAlbumsForAddingAsync(id, userId, ct);
        }

        return Page();
    }

    public Task<IActionResult> OnPostVisibilityAsync(Guid id, bool isPublic, CancellationToken ct) =>
        RunAsync(
            userId => gallery.SetPhotoVisibilityAsync(id, userId, isPublic, ct),
            () => RedirectToPage(new { id, AlbumId }));

    public Task<IActionResult> OnPostAddToAlbumAsync(Guid id, Guid targetAlbumId, CancellationToken ct) =>
        RunAsync(
            userId => gallery.AddPhotoToAlbumAsync(id, targetAlbumId, userId, ct),
            () => RedirectToPage(new { id, AlbumId }));

    public Task<IActionResult> OnPostRemoveFromAlbumAsync(Guid id, Guid fromAlbumId, CancellationToken ct) =>
        RunAsync(
            userId => gallery.RemovePhotoFromAlbumAsync(id, fromAlbumId, userId, ct),
            () => RedirectToPage("/Albums/Details", new { id = fromAlbumId }));

    public Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct) =>
        RunAsync(
            userId => gallery.DeletePhotoAsync(id, userId, ct),
            () => AlbumId is { } albumId
                ? RedirectToPage("/Albums/Details", new { id = albumId })
                : RedirectToPage("/Albums/Index"));
}

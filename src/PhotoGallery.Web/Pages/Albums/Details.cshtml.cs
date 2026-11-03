using Microsoft.AspNetCore.Mvc;
using PhotoGallery.Web.Services;

namespace PhotoGallery.Web.Pages.Albums;

/// <summary>Album detail. Public albums are visible to anonymous visitors; changes require the owner.</summary>
public class DetailsModel(GalleryService gallery) : GalleryPageModel
{
    [BindProperty(SupportsGet = true)]
    public SortOrder Sort { get; set; } = SortOrder.Newest;

    public AlbumDetails Details { get; private set; } = default!;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        var details = await gallery.GetAlbumAsync(id, CurrentUserId, Sort, ct);
        if (details is null)
        {
            return NotFound();
        }

        Details = details;
        return Page();
    }

    public Task<IActionResult> OnPostRenameAsync(Guid id, string? name, CancellationToken ct) =>
        RunAsync(
            userId => gallery.RenameAlbumAsync(id, userId, name, ct),
            () => RedirectToPage(new { id }));

    public Task<IActionResult> OnPostVisibilityAsync(Guid id, bool isPublic, CancellationToken ct) =>
        RunAsync(
            userId => gallery.SetAlbumVisibilityAsync(id, userId, isPublic, ct),
            () => RedirectToPage(new { id }));

    public Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct) =>
        RunAsync(
            userId => gallery.DeleteAlbumAsync(id, userId, ct),
            () => TempData.ContainsKey(SuccessKey)
                ? RedirectToPage("/Albums/Index")
                : RedirectToPage(new { id }));
}

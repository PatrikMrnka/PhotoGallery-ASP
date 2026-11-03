using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoGallery.Web.Services;

namespace PhotoGallery.Web.Pages.Albums;

[Authorize]
public class IndexModel(GalleryService gallery) : GalleryPageModel
{
    [BindProperty(SupportsGet = true)]
    public SortOrder Sort { get; set; } = SortOrder.Newest;

    public IReadOnlyList<AlbumSummary> Albums { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Albums = await gallery.GetUserAlbumsAsync(RequiredUserId, Sort, ct);
    }

    public async Task<IActionResult> OnPostCreateAsync(string? name, CancellationToken ct)
    {
        var (result, albumId) = await gallery.CreateAlbumAsync(RequiredUserId, name, ct);
        if (!result.Succeeded)
        {
            ShowError(result.Message);
            return RedirectToPage();
        }

        ShowSuccess(result.Message);
        return RedirectToPage("/Albums/Details", new { id = albumId });
    }
}

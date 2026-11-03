using Microsoft.AspNetCore.Mvc;
using PhotoGallery.Web.Services;

namespace PhotoGallery.Web.Pages;

public class IndexModel(GalleryService gallery) : GalleryPageModel
{
    public const int PageSize = 24;

    [BindProperty(SupportsGet = true, Name = "p")]
    public int PageNumber { get; set; } = 1;

    public GalleryStats Stats { get; private set; } = new(0, 0);

    public IReadOnlyList<AlbumSummary> Albums { get; private set; } = [];

    public PagedResult<PhotoSummary> Photos { get; private set; } = new([], 1, PageSize, 0);

    public async Task OnGetAsync(CancellationToken ct)
    {
        Stats = await gallery.GetStatsAsync(ct);
        Photos = await gallery.GetPublicPhotosAsync(PageNumber, PageSize, ct);

        if (Photos.Page == 1)
        {
            Albums = await gallery.GetPublicAlbumsAsync(CurrentUserId, take: 8, ct);
        }
    }
}

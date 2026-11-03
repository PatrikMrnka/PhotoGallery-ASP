using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PhotoGallery.Web.Options;
using PhotoGallery.Web.Services;

namespace PhotoGallery.Web.Pages.Photos;

[Authorize]
[RequestSizeLimit(MaxRequestBytes)]
[RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
public class UploadModel(GalleryService gallery, IOptions<UploadOptions> uploadOptions) : GalleryPageModel
{
    // Hard ceiling for one request; the per-file limits in UploadOptions are checked by GalleryService.
    private const long MaxRequestBytes = 300L * 1024 * 1024;

    public UploadOptions Limits => uploadOptions.Value;

    public IReadOnlyList<AlbumSummary> Albums { get; private set; } = [];

    [BindProperty]
    public List<IFormFile> Files { get; set; } = [];

    [BindProperty]
    public bool IsPublic { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? AlbumId { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Albums = await gallery.GetUploadTargetsAsync(RequiredUserId, ct);

        // Photos uploaded into a public album are usually meant to be seen.
        IsPublic = Albums.FirstOrDefault(a => a.Id == AlbumId)?.IsPublic ?? false;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var userId = RequiredUserId;
        var files = Files.Select(f => new UploadFile(f.FileName, f.Length, f.OpenReadStream())).ToList();

        UploadResult result;
        try
        {
            result = await gallery.UploadAsync(userId, files, IsPublic, AlbumId, ct);
        }
        finally
        {
            foreach (var file in files)
            {
                await file.Content.DisposeAsync();
            }
        }

        if (result.Errors.Count > 0)
        {
            ShowError(string.Join(Environment.NewLine, result.Errors));
        }

        if (result.PhotoIds.Count == 0)
        {
            return RedirectToPage(new { AlbumId });
        }

        ShowSuccess(result.PhotoIds.Count == 1 ? "1 photo was uploaded." : $"{result.PhotoIds.Count} photos were uploaded.");

        var targetAlbumId = AlbumId ?? (await gallery.GetOrCreateDefaultAlbumAsync(userId, ct)).Id;
        return RedirectToPage("/Albums/Details", new { id = targetAlbumId });
    }
}

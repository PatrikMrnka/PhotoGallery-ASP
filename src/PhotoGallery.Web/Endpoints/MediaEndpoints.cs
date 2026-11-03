using System.Security.Claims;
using Microsoft.Net.Http.Headers;
using PhotoGallery.Web.Services;

namespace PhotoGallery.Web.Endpoints;

/// <summary>
/// Serves image bytes. Every request goes through <see cref="GalleryService"/>, so private photos
/// are only ever returned to their owner. Hidden and missing photos both return 404.
/// </summary>
public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var media = app.MapGroup("/media");

        media.MapGet("/{id:guid}", async (Guid id, bool? download, HttpContext http, GalleryService gallery) =>
        {
            var image = await gallery.OpenOriginalAsync(id, GetUserId(http.User), http.RequestAborted);
            if (image is null)
            {
                return Results.NotFound();
            }

            SetCacheHeaders(http, image.IsPublic);
            return Results.File(
                image.Content,
                image.ContentType,
                fileDownloadName: download == true ? image.FileName : null,
                enableRangeProcessing: true);
        });

        media.MapGet("/{id:guid}/thumb", async (Guid id, HttpContext http, GalleryService gallery) =>
        {
            var thumbnail = await gallery.GetThumbnailAsync(id, GetUserId(http.User), http.RequestAborted);
            if (thumbnail is null)
            {
                return Results.NotFound();
            }

            SetCacheHeaders(http, thumbnail.IsPublic);
            return Results.File(thumbnail.Data, thumbnail.ContentType);
        });

        return app;
    }

    private static string? GetUserId(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier);

    private static void SetCacheHeaders(HttpContext http, bool isPublic)
    {
        // Short lifetime, because the owner can make a photo private at any time.
        // Private photos must never be stored by shared caches.
        http.Response.Headers[HeaderNames.CacheControl] = isPublic
            ? "public, max-age=300"
            : "private, no-store";
        http.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
    }
}

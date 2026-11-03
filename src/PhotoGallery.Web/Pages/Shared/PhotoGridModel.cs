using PhotoGallery.Web.Services;

namespace PhotoGallery.Web.Pages.Shared;

/// <param name="Photos">Photos to show.</param>
/// <param name="AlbumId">Album the grid belongs to; passed to the detail page for navigation.</param>
/// <param name="ShowVisibility">Marks private photos with a badge (useful for the owner).</param>
/// <param name="EmptyText">Text shown when there are no photos.</param>
public sealed record PhotoGridModel(
    IReadOnlyList<PhotoSummary> Photos,
    Guid? AlbumId = null,
    bool ShowVisibility = false,
    string EmptyText = "No photos yet.");

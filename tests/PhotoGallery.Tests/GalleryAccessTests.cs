using PhotoGallery.Web.Models;
using PhotoGallery.Web.Services;

namespace PhotoGallery.Tests;

public class GalleryAccessTests
{
    private static Photo PhotoOf(string owner, bool isPublic) =>
        new() { OriginalName = "a.png", ContentType = "image/png", OwnerId = owner, IsPublic = isPublic };

    [Theory]
    [InlineData(true, "owner", true)]
    [InlineData(true, "stranger", true)]
    [InlineData(true, null, true)]
    [InlineData(false, "owner", true)]
    [InlineData(false, "stranger", false)]
    [InlineData(false, null, false)]
    public void Photo_visibility(bool isPublic, string? viewer, bool expected)
    {
        Assert.Equal(expected, GalleryAccess.CanView(PhotoOf("owner", isPublic), viewer));
    }

    [Theory]
    [InlineData(true, "owner", true)]
    [InlineData(true, null, true)]
    [InlineData(false, "owner", true)]
    [InlineData(false, "stranger", false)]
    [InlineData(false, null, false)]
    public void Album_visibility(bool isPublic, string? viewer, bool expected)
    {
        var album = new Album { Name = "A", OwnerId = "owner", IsPublic = isPublic };

        Assert.Equal(expected, GalleryAccess.CanView(album, viewer));
    }

    [Fact]
    public void Expression_matches_in_memory_rule()
    {
        var photos = new[] { PhotoOf("owner", true), PhotoOf("owner", false), PhotoOf("other", false) };

        foreach (var viewer in new[] { "owner", "other", null })
        {
            var predicate = GalleryAccess.PhotoVisibleTo(viewer).Compile();
            Assert.Equal(
                photos.Select(p => GalleryAccess.CanView(p, viewer)),
                photos.Select(predicate));
        }
    }

    [Fact]
    public void Owner_id_comparison_is_exact()
    {
        Assert.False(GalleryAccess.IsOwner("Owner", "owner"));
        Assert.False(GalleryAccess.IsOwner("owner", null));
    }
}

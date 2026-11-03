using PhotoGallery.Tests.Infrastructure;
using PhotoGallery.Web.Options;
using PhotoGallery.Web.Services;
using SkiaSharp;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace PhotoGallery.Tests;

public class ImageProcessorTests
{
    private static SkiaImageProcessor CreateProcessor(UploadOptions? options = null) =>
        new(MsOptions.Create(options ?? new UploadOptions()));

    [Fact]
    public async Task Detects_the_real_format_and_keeps_dimensions()
    {
        var result = await CreateProcessor().ProcessAsync(new MemoryStream(TestImages.Png(64, 48)));

        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(64, result.Width);
        Assert.Equal(48, result.Height);
        Assert.Equal(SKEncodedImageFormat.Png, TestImages.Inspect(result.Data).Format);
    }

    [Fact]
    public async Task Removes_exif_metadata()
    {
        var input = TestImages.JpegWithExif();
        Assert.True(TestImages.ContainsExif(input));

        var result = await CreateProcessor().ProcessAsync(new MemoryStream(input));

        Assert.Equal("image/jpeg", result.ContentType);
        Assert.False(TestImages.ContainsExif(result.Data));
    }

    [Fact]
    public async Task Applies_exif_orientation_to_the_pixels()
    {
        // Orientation 6 = the camera was rotated, the picture must be turned 90° clockwise.
        var input = TestImages.JpegWithExif(64, 48, orientation: 6);

        var result = await CreateProcessor().ProcessAsync(new MemoryStream(input));

        Assert.Equal((48, 64), (result.Width, result.Height));
        var stored = TestImages.Inspect(result.Data);
        Assert.Equal((48, 64), (stored.Width, stored.Height));
    }

    [Fact]
    public async Task Creates_webp_thumbnail_limited_to_configured_size()
    {
        var processor = CreateProcessor(new UploadOptions { ThumbnailSize = 100 });

        var result = await processor.ProcessAsync(new MemoryStream(TestImages.Png(400, 200)));

        var thumbnail = TestImages.Inspect(result.Thumbnail);
        Assert.Equal("image/webp", result.ThumbnailContentType);
        Assert.Equal(SKEncodedImageFormat.Webp, thumbnail.Format);
        Assert.Equal((100, 50), (thumbnail.Width, thumbnail.Height));
    }

    [Fact]
    public async Task Does_not_upscale_small_images_for_thumbnails()
    {
        var result = await CreateProcessor().ProcessAsync(new MemoryStream(TestImages.Png(40, 30)));

        Assert.Equal(40, TestImages.Inspect(result.Thumbnail).Width);
    }

    [Fact]
    public async Task Keeps_gif_files_unchanged()
    {
        var result = await CreateProcessor().ProcessAsync(new MemoryStream(TestImages.Gif));

        Assert.Equal("image/gif", result.ContentType);
        Assert.Equal(TestImages.Gif, result.Data);
        Assert.Equal(SKEncodedImageFormat.Webp, TestImages.Inspect(result.Thumbnail).Format);
    }

    [Fact]
    public async Task Rejects_unsupported_formats()
    {
        var exception = await Assert.ThrowsAsync<InvalidImageException>(
            () => CreateProcessor().ProcessAsync(new MemoryStream(TestImages.Bmp())));

        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public async Task Rejects_random_bytes()
    {
        await Assert.ThrowsAsync<InvalidImageException>(
            () => CreateProcessor().ProcessAsync(new MemoryStream([1, 2, 3, 4, 5, 6, 7, 8])));
    }

    [Fact]
    public async Task Rejects_images_with_too_many_pixels()
    {
        var processor = CreateProcessor(new UploadOptions { MaxPixels = 1000 });

        await Assert.ThrowsAsync<InvalidImageException>(
            () => processor.ProcessAsync(new MemoryStream(TestImages.Png(100, 100))));
    }
}

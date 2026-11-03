using PhotoGallery.Tests.Infrastructure;
using PhotoGallery.Web.Options;
using PhotoGallery.Web.Services;
using SixLabors.ImageSharp;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace PhotoGallery.Tests;

public class ImageProcessorTests
{
    private static ImageSharpProcessor CreateProcessor(UploadOptions? options = null) =>
        new(MsOptions.Create(options ?? new UploadOptions()));

    [Fact]
    public async Task Detects_the_real_format_and_keeps_dimensions()
    {
        var result = await CreateProcessor().ProcessAsync(new MemoryStream(TestImages.Png(64, 48)));

        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(64, result.Width);
        Assert.Equal(48, result.Height);
    }

    [Fact]
    public async Task Removes_exif_metadata_including_location()
    {
        var input = TestImages.JpegWithExif();
        Assert.NotNull(Image.Identify(input).Metadata.ExifProfile);

        var result = await CreateProcessor().ProcessAsync(new MemoryStream(input));

        Assert.Null(Image.Identify(result.Data).Metadata.ExifProfile);
    }

    [Fact]
    public async Task Creates_webp_thumbnail_limited_to_configured_size()
    {
        var processor = CreateProcessor(new UploadOptions { ThumbnailSize = 100 });

        var result = await processor.ProcessAsync(new MemoryStream(TestImages.Png(400, 200)));

        var thumbnail = Image.Identify(result.Thumbnail);
        Assert.Equal("image/webp", result.ThumbnailContentType);
        Assert.Equal(100, thumbnail.Width);
        Assert.Equal(50, thumbnail.Height);
    }

    [Fact]
    public async Task Does_not_upscale_small_images_for_thumbnails()
    {
        var result = await CreateProcessor().ProcessAsync(new MemoryStream(TestImages.Png(40, 30)));

        Assert.Equal(40, Image.Identify(result.Thumbnail).Width);
    }

    [Fact]
    public async Task Rejects_unsupported_formats()
    {
        await Assert.ThrowsAsync<InvalidImageException>(
            () => CreateProcessor().ProcessAsync(new MemoryStream(TestImages.Bmp())));
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

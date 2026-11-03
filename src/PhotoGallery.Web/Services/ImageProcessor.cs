using Microsoft.Extensions.Options;
using PhotoGallery.Web.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace PhotoGallery.Web.Services;

public sealed record ProcessedImage(
    byte[] Data,
    string ContentType,
    int Width,
    int Height,
    byte[] Thumbnail,
    string ThumbnailContentType);

/// <summary>Thrown when an uploaded file is not an acceptable image. The message is safe to show to the user.</summary>
public sealed class InvalidImageException(string message) : Exception(message);

public interface IImageProcessor
{
    /// <summary>
    /// Validates the image, normalizes its orientation, strips metadata (EXIF incl. GPS position)
    /// and creates a thumbnail.
    /// </summary>
    Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken cancellationToken = default);
}

public sealed class ImageSharpProcessor(IOptions<UploadOptions> options) : IImageProcessor
{
    private static readonly IImageFormat[] AllowedFormats =
    [
        JpegFormat.Instance,
        PngFormat.Instance,
        GifFormat.Instance,
        WebpFormat.Instance,
    ];

    private readonly UploadOptions _options = options.Value;

    public async Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken cancellationToken = default)
    {
        // The content type sent by the browser cannot be trusted, so the real format is detected from the bytes.
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);

        var format = await DetectFormatAsync(buffer, cancellationToken);

        buffer.Position = 0;
        ImageInfo info;
        try
        {
            info = await Image.IdentifyAsync(buffer, cancellationToken);
        }
        catch (InvalidImageContentException)
        {
            throw new InvalidImageException("The image file is damaged and cannot be read.");
        }

        if ((long)info.Width * info.Height > _options.MaxPixels)
        {
            throw new InvalidImageException(
                $"The image resolution is too large (maximum is {_options.MaxPixels / 1_000_000} megapixels).");
        }

        buffer.Position = 0;
        using var image = await LoadAsync(buffer, cancellationToken);

        image.Mutate(x => x.AutoOrient());
        image.Metadata.ExifProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;

        using var original = new MemoryStream();
        var encoder = image.Configuration.ImageFormatsManager.GetEncoder(format);
        await image.SaveAsync(original, encoder, cancellationToken);

        using var thumbnail = image.Frames.CloneFrame(0);
        if (thumbnail.Width > _options.ThumbnailSize || thumbnail.Height > _options.ThumbnailSize)
        {
            thumbnail.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(_options.ThumbnailSize, _options.ThumbnailSize),
                Mode = ResizeMode.Max,
            }));
        }

        using var thumbnailStream = new MemoryStream();
        await thumbnail.SaveAsWebpAsync(
            thumbnailStream,
            new WebpEncoder { FileFormat = WebpFileFormatType.Lossy, Quality = 80 },
            cancellationToken);

        return new ProcessedImage(
            original.ToArray(),
            format.DefaultMimeType,
            image.Width,
            image.Height,
            thumbnailStream.ToArray(),
            WebpFormat.Instance.DefaultMimeType);
    }

    private static async Task<IImageFormat> DetectFormatAsync(Stream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        IImageFormat format;
        try
        {
            format = await Image.DetectFormatAsync(stream, cancellationToken);
        }
        catch (UnknownImageFormatException)
        {
            throw new InvalidImageException("The file is not a supported image. Use JPEG, PNG, GIF or WebP.");
        }

        if (!AllowedFormats.Contains(format))
        {
            throw new InvalidImageException($"{format.Name} images are not supported. Use JPEG, PNG, GIF or WebP.");
        }

        return format;
    }

    private static async Task<Image> LoadAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            return await Image.LoadAsync(stream, cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidImageContentException or UnknownImageFormatException)
        {
            throw new InvalidImageException("The image file is damaged and cannot be read.");
        }
    }
}

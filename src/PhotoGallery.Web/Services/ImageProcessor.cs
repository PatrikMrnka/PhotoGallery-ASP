using Microsoft.Extensions.Options;
using PhotoGallery.Web.Options;
using SkiaSharp;

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

/// <summary>Image processing based on SkiaSharp (Google Skia).</summary>
public sealed class SkiaImageProcessor(IOptions<UploadOptions> options) : IImageProcessor
{
    private const int OriginalQuality = 90;
    private const int ThumbnailQuality = 80;

    private static readonly Dictionary<SKEncodedImageFormat, string> AllowedFormats = new()
    {
        [SKEncodedImageFormat.Jpeg] = "image/jpeg",
        [SKEncodedImageFormat.Png] = "image/png",
        [SKEncodedImageFormat.Gif] = "image/gif",
        [SKEncodedImageFormat.Webp] = "image/webp",
    };

    private readonly UploadOptions _options = options.Value;

    public async Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken cancellationToken = default)
    {
        // The content type sent by the browser cannot be trusted, so the real format is detected from the bytes.
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data)
            ?? throw new InvalidImageException("The file is not a supported image. Use JPEG, PNG, GIF or WebP.");

        if (!AllowedFormats.TryGetValue(codec.EncodedFormat, out var contentType))
        {
            throw new InvalidImageException($"{codec.EncodedFormat} images are not supported. Use JPEG, PNG, GIF or WebP.");
        }

        // Checked before decoding: the header is enough, so huge images never allocate memory (decompression bombs).
        if ((long)codec.Info.Width * codec.Info.Height > _options.MaxPixels)
        {
            throw new InvalidImageException(
                $"The image resolution is too large (maximum is {_options.MaxPixels / 1_000_000} megapixels).");
        }

        using var decoded = SKBitmap.Decode(codec)
            ?? throw new InvalidImageException("The image file is damaged and cannot be read.");

        // EXIF orientation is applied to the pixels, because the stored file no longer carries EXIF data.
        // If the decoder already did it (the dimensions differ from the header), nothing is left to do.
        var origin = decoded.Width == codec.Info.Width && decoded.Height == codec.Info.Height
            ? codec.EncodedOrigin
            : SKEncodedOrigin.TopLeft;
        using var image = ApplyOrientation(decoded, origin);

        byte[] original;
        if (codec.EncodedFormat == SKEncodedImageFormat.Gif)
        {
            // Skia cannot write GIF. GIF files carry no EXIF data, so the original (incl. animation) is kept.
            original = bytes;
        }
        else
        {
            // Re-encoding writes only pixels, which removes EXIF, GPS and other metadata.
            original = Encode(image, codec.EncodedFormat, OriginalQuality);
        }

        using var thumbnail = CreateThumbnail(image, _options.ThumbnailSize);
        var thumbnailBytes = Encode(thumbnail ?? image, SKEncodedImageFormat.Webp, ThumbnailQuality);

        return new ProcessedImage(original, contentType, image.Width, image.Height, thumbnailBytes, "image/webp");
    }

    private static byte[] Encode(SKBitmap bitmap, SKEncodedImageFormat format, int quality)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(format, quality)
            ?? throw new InvalidImageException("The image could not be processed.");
        return encoded.ToArray();
    }

    /// <summary>Returns a downscaled copy, or <c>null</c> when the image is already small enough.</summary>
    private static SKBitmap? CreateThumbnail(SKBitmap source, int maxSize)
    {
        if (source.Width <= maxSize && source.Height <= maxSize)
        {
            return null;
        }

        var scale = Math.Min((double)maxSize / source.Width, (double)maxSize / source.Height);
        var info = new SKImageInfo(
            Math.Max(1, (int)Math.Round(source.Width * scale)),
            Math.Max(1, (int)Math.Round(source.Height * scale)),
            SKColorType.Rgba8888,
            source.AlphaType == SKAlphaType.Opaque ? SKAlphaType.Opaque : SKAlphaType.Premul);

        return source.Resize(info, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            ?? throw new InvalidImageException("The image could not be processed.");
    }

    /// <summary>Rotates or mirrors the pixels so the image is shown upright without relying on EXIF orientation.</summary>
    internal static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        var swapsSides = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        var width = swapsSides ? source.Height : source.Width;
        var height = swapsSides ? source.Width : source.Height;
        var alphaType = source.AlphaType == SKAlphaType.Opaque ? SKAlphaType.Opaque : SKAlphaType.Premul;
        var result = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, alphaType));

        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);
        switch (origin)
        {
            case SKEncodedOrigin.TopRight: // mirrored horizontally
                canvas.Translate(width, 0);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.BottomRight: // rotated 180°
                canvas.Translate(width, height);
                canvas.Scale(-1, -1);
                break;
            case SKEncodedOrigin.BottomLeft: // mirrored vertically
                canvas.Translate(0, height);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.LeftTop: // transposed
                canvas.RotateDegrees(90);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.RightTop: // rotated 90° clockwise
                canvas.Translate(width, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom: // transverse
                canvas.Translate(width, height);
                canvas.Scale(-1, -1);
                canvas.RotateDegrees(90);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.LeftBottom: // rotated 90° counter-clockwise
                canvas.Translate(0, height);
                canvas.RotateDegrees(-90);
                break;
        }

        using var sourceImage = SKImage.FromBitmap(source);
        canvas.DrawImage(sourceImage, 0, 0);
        return result;
    }
}

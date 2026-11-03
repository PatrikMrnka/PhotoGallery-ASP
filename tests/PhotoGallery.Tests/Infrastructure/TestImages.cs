using System.Buffers.Binary;
using SkiaSharp;

namespace PhotoGallery.Tests.Infrastructure;

internal static class TestImages
{
    /// <summary>The smallest valid GIF: 1×1 pixel.</summary>
    public static readonly byte[] Gif = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

    public static byte[] Png(int width = 64, int height = 48) =>
        Encode(width, height, SKEncodedImageFormat.Png);

    public static byte[] Jpeg(int width = 64, int height = 48) =>
        Encode(width, height, SKEncodedImageFormat.Jpeg);

    /// <summary>A JPEG with an EXIF block containing the given orientation (1 = upright, 6 = rotate 90° clockwise).</summary>
    public static byte[] JpegWithExif(int width = 64, int height = 48, ushort orientation = 1)
    {
        var jpeg = Jpeg(width, height);

        // Minimal little-endian TIFF structure with one IFD entry: Orientation (tag 0x0112, type SHORT).
        var tiff = new byte[26];
        "II"u8.CopyTo(tiff);
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(2), 42);
        BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(4), 8);
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(8), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(10), 0x0112);
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(12), 3);
        BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(14), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(18), orientation);

        byte[] payload = [.. ExifHeader, .. tiff];
        var segmentLength = (ushort)(payload.Length + 2);
        byte[] app1 = [0xFF, 0xE1, (byte)(segmentLength >> 8), (byte)segmentLength, .. payload];

        // Insert the APP1 (EXIF) segment right after the JPEG start-of-image marker.
        return [.. jpeg.AsSpan(0, 2), .. app1, .. jpeg.AsSpan(2)];
    }

    /// <summary>A valid 1×1 24-bit BMP file (a format the gallery does not accept).</summary>
    public static byte[] Bmp()
    {
        var bmp = new byte[58];
        "BM"u8.CopyTo(bmp);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(2), bmp.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10), 54);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18), 1);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22), 1);
        BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(26), 1);
        BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(28), 24);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(34), 4);
        bmp[54] = 255;
        return bmp;
    }

    public static bool ContainsExif(byte[] data) => data.AsSpan().IndexOf(ExifHeader) >= 0;

    public static (int Width, int Height, SKEncodedImageFormat Format) Inspect(byte[] data)
    {
        using var skData = SKData.CreateCopy(data);
        using var codec = SKCodec.Create(skData) ?? throw new InvalidOperationException("Not an image.");
        return (codec.Info.Width, codec.Info.Height, codec.EncodedFormat);
    }

    private static ReadOnlySpan<byte> ExifHeader => "Exif\0\0"u8;

    private static byte[] Encode(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(new SKColor(30, 120, 200));
            using var paint = new SKPaint { Color = new SKColor(240, 180, 40) };
            canvas.DrawRect(0, 0, width / 2f, height / 3f, paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }
}

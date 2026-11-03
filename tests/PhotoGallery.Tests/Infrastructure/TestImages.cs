using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace PhotoGallery.Tests.Infrastructure;

internal static class TestImages
{
    public static byte[] Png(int width = 64, int height = 48)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(30, 120, 200));
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }

    public static byte[] JpegWithExif(int width = 64, int height = 48)
    {
        using var image = new Image<Rgb24>(width, height, new Rgb24(200, 120, 30));
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Software, "PhotoGallery tests");
        exif.SetValue(ExifTag.GPSLatitudeRef, "N");
        image.Metadata.ExifProfile = exif;

        using var stream = new MemoryStream();
        image.Save(stream, new JpegEncoder());
        return stream.ToArray();
    }

    public static byte[] Bmp()
    {
        using var image = new Image<Rgb24>(8, 8);
        using var stream = new MemoryStream();
        image.Save(stream, new BmpEncoder());
        return stream.ToArray();
    }
}

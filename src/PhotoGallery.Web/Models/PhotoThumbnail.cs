using System.ComponentModel.DataAnnotations;

namespace PhotoGallery.Web.Models;

/// <summary>
/// Downscaled preview of a photo. Kept in its own table so that listing photos never loads image bytes.
/// </summary>
public class PhotoThumbnail
{
    public Guid PhotoId { get; set; }

    public Photo? Photo { get; set; }

    public required byte[] Data { get; set; }

    [MaxLength(100)]
    public required string ContentType { get; set; }
}

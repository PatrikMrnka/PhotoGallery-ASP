using System.ComponentModel.DataAnnotations;

namespace PhotoGallery.Web.Models;

public class Photo
{
    public Guid Id { get; set; }

    /// <summary>File name as uploaded by the user. Only used for display and downloads.</summary>
    [MaxLength(255)]
    public required string OriginalName { get; set; }

    [MaxLength(100)]
    public required string ContentType { get; set; }

    public long SizeBytes { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public DateTime UploadedAt { get; set; }

    public bool IsPublic { get; set; }

    public required string OwnerId { get; set; }

    public ApplicationUser? Owner { get; set; }

    public PhotoThumbnail? Thumbnail { get; set; }

    public ICollection<AlbumPhoto> AlbumPhotos { get; set; } = new List<AlbumPhoto>();
}

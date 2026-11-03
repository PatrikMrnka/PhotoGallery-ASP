using System.ComponentModel.DataAnnotations;

namespace PhotoGallery.Web.Models;

public class Album
{
    public const int NameMaxLength = 100;

    public Guid Id { get; set; }

    [MaxLength(NameMaxLength)]
    public required string Name { get; set; }

    public bool IsPublic { get; set; }

    /// <summary>
    /// Every user has exactly one default album that automatically contains all of their uploads.
    /// It cannot be renamed, published or deleted.
    /// </summary>
    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public required string OwnerId { get; set; }

    public ApplicationUser? Owner { get; set; }

    public ICollection<AlbumPhoto> AlbumPhotos { get; set; } = new List<AlbumPhoto>();
}

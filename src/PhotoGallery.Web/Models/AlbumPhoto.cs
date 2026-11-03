namespace PhotoGallery.Web.Models;

/// <summary>Many-to-many link between albums and photos. A photo can be in several albums.</summary>
public class AlbumPhoto
{
    public Guid AlbumId { get; set; }

    public Album? Album { get; set; }

    public Guid PhotoId { get; set; }

    public Photo? Photo { get; set; }

    public DateTime AddedAt { get; set; }
}

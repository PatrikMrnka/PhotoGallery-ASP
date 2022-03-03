namespace _2021p3web_gallery_PatrikMrnka.Models
{
    public class StoredFileAlbum
    {
        public Guid? AlbumId { get; set; }
        public Album Album { get; set; }

        public Guid? PictureId { get; set; }
        public StoredFile Picture { get; set; }
    }
}

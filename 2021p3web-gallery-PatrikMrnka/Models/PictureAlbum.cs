namespace _2021p3web_gallery_PatrikMrnka.Models
{
    public class PictureAlbum
    {
        public Guid PictureId { get; set; }
        public Picture Picture { get; set; }

        public Guid AlbumId { get; set; }
        public Album Album { get; set; }
    }
}

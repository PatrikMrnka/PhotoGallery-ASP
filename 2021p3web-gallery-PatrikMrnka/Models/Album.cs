using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2021p3web_gallery_PatrikMrnka.Models
{
    public class Album
    {
        [Key]
        public Guid AlbumId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public bool IsPublic { get; set; }
        public DateTime CreationDate { get; set; }
        [ForeignKey("UploaderId")]
        public ApplicationUser Uploader { get; set; }
        public ICollection<PictureAlbum> PictureAlbums { get; set; }
    }
}

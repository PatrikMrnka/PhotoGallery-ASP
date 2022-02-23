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
        public bool IsPublic { get; set; }
        public DateTime CreationDate { get; set; }

        [ForeignKey("UploaderId")]
        public ApplicationUser Uploader { get; set; } // kdo soubor nahrál
        [Required]
        public string UploaderId { get; set; } // identifikátor uživatele, který soubor nahrál
        public ICollection<StoredFileAlbum> StoredFileAlbums { get; set; }
    }
}

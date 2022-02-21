using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2021p3web_gallery_PatrikMrnka.Models
{
    public class Picture
    {
        [Key]
        public Guid Id { get; set; } // identifikátor souboru a název fyzického souboru
        [Required]
        public string UploaderId { get; set; } // identifikátor uživatele, který soubor nahrál
        [Required]
        public DateTime UploadedAt { get; set; } // datum a čas nahrání souboru
        [Required]
        public string OriginalName { get; set; } // původní název souboru
        [Required]
        public string ContentType { get; set; } // druh obsahu v souboru (MIME type)
        [Required]
        public bool IsPublic { get; set; }

        [ForeignKey("UploaderId")]
        public ApplicationUser Uploader { get; set; } // kdo soubor nahrál
        public ICollection<Thumbnail> Thumbnails { get; set; } // kolekce všech možných náhledů
        public ICollection<PictureAlbum> PictureAlbums { get; set; }
    }
}

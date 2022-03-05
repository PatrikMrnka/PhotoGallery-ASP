using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2021p3web_gallery_PatrikMrnka.Models
{
    public class StoredFile
    {
        [Key]
        public Guid? Id { get; set; } // identifikátor souboru a název fyzického souboru
        [Required]
        public DateTime UploadedAt { get; set; } // datum a čas nahrání souboru
        [Required]
        public string OriginalName { get; set; } // původní název souboru
        [Required]
        public string ContentType { get; set; } // druh obsahu v souboru (MIME type)
        public bool IsPublic { get; set; } // je obrázek veřejný?
        public ICollection<Thumbnail> Thumbnails { get; set; } // kolekce všech možných náhledů
        [ForeignKey("UploaderId")]
        public ApplicationUser Uploader { get; set; } // kdo soubor nahrál
        [Required]
        public string UploaderId { get; set; } // identifikátor uživatele, který soubor nahrál

        public ICollection<StoredFileAlbum> StoredFileAlbums { get; set; }
    }
}

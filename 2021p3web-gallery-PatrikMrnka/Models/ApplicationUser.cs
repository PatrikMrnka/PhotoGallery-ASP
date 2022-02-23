using Microsoft.AspNetCore.Identity;

namespace _2021p3web_gallery_PatrikMrnka.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<StoredFile> Pictures { get; set; }
        public ICollection<Album> Albums { get; set; }
        public ICollection<StoredFileAlbum> StoredFileAlbums { get; set; }
    }
}

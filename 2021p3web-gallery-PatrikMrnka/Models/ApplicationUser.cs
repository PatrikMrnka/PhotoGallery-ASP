using Microsoft.AspNetCore.Identity;

namespace _2021p3web_gallery_PatrikMrnka.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Name { get; set; }
        public DateTime Birthday { get; set; }
        public ICollection<Picture> Pictures { get; set; }
        public ICollection<Album> Albums { get; set; }
    }
}

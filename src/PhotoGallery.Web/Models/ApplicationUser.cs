using Microsoft.AspNetCore.Identity;

namespace PhotoGallery.Web.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();

    public ICollection<Album> Albums { get; set; } = new List<Album>();
}

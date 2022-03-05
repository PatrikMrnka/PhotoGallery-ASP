using _2021p3web_gallery_PatrikMrnka.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace _2021p3web_gallery_PatrikMrnka.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<StoredFile> Files { get; set; }
        public DbSet<Album> Albums { get; set; }
        public DbSet<Thumbnail> Thumbnails { get; set; }
        public DbSet<StoredFileAlbum> StoredFileAlbums { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            base.OnModelCreating(builder);
            //builder.Entity<Thumbnail>().HasKey(t => new { t.FileId, t.Type });

            builder.Entity<StoredFileAlbum>().HasKey(a => new { a.PictureId, a.AlbumId });

            builder.Entity<StoredFileAlbum>()
                .HasOne(a => a.Picture)
                .WithMany(a => a.StoredFileAlbums)
                .HasForeignKey(b => b.PictureId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StoredFileAlbum>()
                .HasOne(a => a.Album)
                .WithMany(a => a.StoredFileAlbums)
                .HasForeignKey(b => b.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StoredFile>()
                .HasOne(a => a.Uploader)
                .WithMany(a => a.Pictures)
                .HasForeignKey(b => b.UploaderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Album>()
                .HasOne(a => a.Uploader)
                .WithMany(a => a.Albums)
                .HasForeignKey(b => b.UploaderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<IdentityRole>().HasData(new IdentityRole
            {
                Id = "ae605389-4f99-42ac-a62e-7abc67bdd298",
                Name = "Administator",
                NormalizedName = "ADMINISTRATOR"
            });

            builder.Entity<ApplicationUser>().HasData(new ApplicationUser
            {
                Id = "6540d112-4e61-48da-9236-06d12567613e",
                Email = "administrator@gmail.com",
                NormalizedEmail = "ADMINISTRATOR@GMAIL.COM",
                UserName = "administrator@gmail.com",
                NormalizedUserName = "ADMINISTRATOR@GMAIL.COM",
                EmailConfirmed = true,
                PasswordHash = hasher.HashPassword(null, "Admin01"),
                LockoutEnabled = false,
                SecurityStamp = String.Empty
            });
            builder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
            {
                RoleId = "ae605389-4f99-42ac-a62e-7abc67bdd298",
                UserId = "6540d112-4e61-48da-9236-06d12567613e"
            });
        }
    }
}
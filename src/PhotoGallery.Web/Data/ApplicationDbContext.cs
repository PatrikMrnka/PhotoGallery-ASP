using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PhotoGallery.Web.Models;

namespace PhotoGallery.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Photo> Photos => Set<Photo>();

    public DbSet<PhotoThumbnail> Thumbnails => Set<PhotoThumbnail>();

    public DbSet<Album> Albums => Set<Album>();

    public DbSet<AlbumPhoto> AlbumPhotos => Set<AlbumPhoto>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Photo>(photo =>
        {
            photo.HasOne(p => p.Owner)
                .WithMany(u => u.Photos)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            photo.HasOne(p => p.Thumbnail)
                .WithOne(t => t.Photo)
                .HasForeignKey<PhotoThumbnail>(t => t.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);

            photo.HasIndex(p => new { p.IsPublic, p.UploadedAt });
            photo.HasIndex(p => p.OwnerId);
        });

        builder.Entity<PhotoThumbnail>().HasKey(t => t.PhotoId);

        builder.Entity<Album>(album =>
        {
            album.HasOne(a => a.Owner)
                .WithMany(u => u.Albums)
                .HasForeignKey(a => a.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // At most one default album per user.
            album.HasIndex(a => a.OwnerId)
                .IsUnique()
                .HasFilter("\"IsDefault\" = 1")
                .HasDatabaseName("IX_Albums_OwnerId_Default");

            album.HasIndex(a => new { a.IsPublic, a.CreatedAt });
        });

        builder.Entity<AlbumPhoto>(link =>
        {
            link.HasKey(ap => new { ap.AlbumId, ap.PhotoId });

            link.HasOne(ap => ap.Album)
                .WithMany(a => a.AlbumPhotos)
                .HasForeignKey(ap => ap.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);

            link.HasOne(ap => ap.Photo)
                .WithMany(p => p.AlbumPhotos)
                .HasForeignKey(ap => ap.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

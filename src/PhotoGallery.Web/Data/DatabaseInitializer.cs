using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoGallery.Web.Models;
using PhotoGallery.Web.Options;
using PhotoGallery.Web.Services;

namespace PhotoGallery.Web.Data;

/// <summary>Applies migrations on startup and, in demo mode, seeds the shared demo account.</summary>
public static class DatabaseInitializer
{
    private sealed record SeedPhoto(string File, bool IsPublic, string? Album);

    private sealed record SeedAlbum(string Name, bool IsPublic);

    private static readonly SeedAlbum[] DemoAlbums =
    [
        new("Mountains", IsPublic: true),
        new("Sea & coast", IsPublic: true),
        new("Night sky", IsPublic: true),
        new("Drafts", IsPublic: false),
    ];

    private static readonly SeedPhoto[] DemoPhotos =
    [
        new("alpine-sunrise.jpg", true, "Mountains"),
        new("misty-ridges.jpg", true, "Mountains"),
        new("desert-dunes.jpg", true, null),
        new("calm-sea.jpg", true, "Sea & coast"),
        new("lighthouse-dusk.jpg", true, "Sea & coast"),
        new("aurora.jpg", true, "Night sky"),
        new("milky-way.jpg", true, "Night sky"),
        new("forest-lake.jpg", true, null),
        new("autumn-hills.jpg", false, "Drafts"),
        new("city-lights.jpg", false, "Drafts"),
    ];

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var storage = provider.GetRequiredService<IPhotoStorage>();
        var demo = provider.GetRequiredService<IOptions<DemoOptions>>().Value;

        if (demo.Enabled && demo.ResetOnStartup)
        {
            logger.LogInformation("Demo reset: deleting database and uploaded files");
            await db.Database.EnsureDeletedAsync();
            storage.DeleteAll();
        }

        await db.Database.MigrateAsync();

        var gallery = provider.GetRequiredService<GalleryService>();
        var orphans = await gallery.DeleteOrphanedFilesAsync();
        if (orphans > 0)
        {
            logger.LogInformation("Deleted {Count} orphaned files from storage", orphans);
        }

        if (demo.Enabled)
        {
            var environment = provider.GetRequiredService<IWebHostEnvironment>();
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            await SeedDemoAsync(db, gallery, userManager, demo, environment.ContentRootPath, logger);
        }
    }

    private static async Task SeedDemoAsync(
        ApplicationDbContext db,
        GalleryService gallery,
        UserManager<ApplicationUser> userManager,
        DemoOptions demo,
        string contentRoot,
        ILogger logger)
    {
        var user = await userManager.FindByEmailAsync(demo.Email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = demo.Email,
                Email = demo.Email,
                EmailConfirmed = true,
                LockoutEnabled = false,
            };

            var created = await userManager.CreateAsync(user, demo.Password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not create the demo user: " + string.Join(" ", created.Errors.Select(e => e.Description)));
            }
        }

        if (await db.Photos.AnyAsync(p => p.OwnerId == user.Id))
        {
            return;
        }

        var albumIds = new Dictionary<string, Guid>();
        foreach (var album in DemoAlbums)
        {
            var (_, id) = await gallery.CreateAlbumAsync(user.Id, album.Name);
            albumIds[album.Name] = id!.Value;
        }

        var seedDirectory = Path.Combine(contentRoot, "SeedData");
        foreach (var photo in DemoPhotos)
        {
            var path = Path.Combine(seedDirectory, photo.File);
            if (!File.Exists(path))
            {
                logger.LogWarning("Demo photo {Path} is missing", path);
                continue;
            }

            await using var stream = File.OpenRead(path);
            Guid? albumId = photo.Album is null ? null : albumIds[photo.Album];
            var result = await gallery.UploadAsync(
                user.Id,
                [new UploadFile(photo.File, stream.Length, stream)],
                photo.IsPublic,
                albumId);

            foreach (var error in result.Errors)
            {
                logger.LogWarning("Demo photo was not uploaded: {Error}", error);
            }
        }

        foreach (var album in DemoAlbums.Where(a => a.IsPublic))
        {
            await gallery.SetAlbumVisibilityAsync(albumIds[album.Name], user.Id, isPublic: true);
        }

        logger.LogInformation("Seeded demo account {Email}", demo.Email);
    }
}

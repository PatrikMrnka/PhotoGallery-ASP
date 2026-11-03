using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PhotoGallery.Web.Models;
using PhotoGallery.Web.Services;

namespace PhotoGallery.Tests.Infrastructure;

/// <summary>Runs the real application with its own temporary database and upload folder.</summary>
public sealed class GalleryWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "photogallery-tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={Path.Combine(_root, "test.db")}");
        builder.UseSetting("Storage:Path", Path.Combine(_root, "uploads"));
        builder.UseSetting("Demo:Enabled", "false");
    }

    public async Task<string> CreateUserAsync(string email)
    {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await users.CreateAsync(user, "Password123");
        Assert.True(result.Succeeded, string.Join(" ", result.Errors.Select(e => e.Description)));
        return user.Id;
    }

    public async Task<Guid> UploadAsync(string userId, bool isPublic)
    {
        using var scope = Services.CreateScope();
        var gallery = scope.ServiceProvider.GetRequiredService<GalleryService>();
        var data = TestImages.Png();
        var result = await gallery.UploadAsync(
            userId, [new UploadFile("test.png", data.Length, new MemoryStream(data))], isPublic, albumId: null);
        return Assert.Single(result.PhotoIds);
    }

    public async Task<Guid> CreateAlbumAsync(string userId, bool isPublic)
    {
        using var scope = Services.CreateScope();
        var gallery = scope.ServiceProvider.GetRequiredService<GalleryService>();
        var (_, albumId) = await gallery.CreateAlbumAsync(userId, "Album");
        if (isPublic)
        {
            await gallery.SetAlbumVisibilityAsync(albumId!.Value, userId, isPublic: true);
        }

        return albumId!.Value;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // SQLite may still hold the file for a moment; the OS cleans the temp folder eventually.
        }
    }
}

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoGallery.Web.Data;
using PhotoGallery.Web.Models;
using PhotoGallery.Web.Options;
using PhotoGallery.Web.Services;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace PhotoGallery.Tests.Infrastructure;

/// <summary>
/// A <see cref="GalleryService"/> backed by an in-memory SQLite database (same provider as production)
/// and in-memory file storage. Contains two users: Alice and Bob.
/// </summary>
internal sealed class GalleryTestContext : IAsyncDisposable
{
    public const string Alice = "alice-id";
    public const string Bob = "bob-id";

    private readonly SqliteConnection _connection;

    private GalleryTestContext(SqliteConnection connection, ApplicationDbContext db, UploadOptions uploadOptions)
    {
        _connection = connection;
        Db = db;
        Service = new GalleryService(
            db,
            Storage,
            new ImageSharpProcessor(MsOptions.Create(uploadOptions)),
            MsOptions.Create(uploadOptions),
            TimeProvider.System,
            NullLogger<GalleryService>.Instance);
    }

    public ApplicationDbContext Db { get; }

    public InMemoryPhotoStorage Storage { get; } = new();

    public GalleryService Service { get; }

    public static async Task<GalleryTestContext> CreateAsync(UploadOptions? uploadOptions = null)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        db.Users.AddRange(
            new ApplicationUser { Id = Alice, UserName = "alice@example.com", Email = "alice@example.com" },
            new ApplicationUser { Id = Bob, UserName = "bob@example.com", Email = "bob@example.com" });
        await db.SaveChangesAsync();

        return new GalleryTestContext(connection, db, uploadOptions ?? new UploadOptions());
    }

    public async Task<Guid> UploadAsync(string userId, bool isPublic = false, Guid? albumId = null, string name = "photo.png")
    {
        var data = TestImages.Png();
        var result = await Service.UploadAsync(
            userId,
            [new UploadFile(name, data.Length, new MemoryStream(data))],
            isPublic,
            albumId);

        Assert.Empty(result.Errors);
        return Assert.Single(result.PhotoIds);
    }

    public async Task<Guid> CreateAlbumAsync(string userId, string name = "Holiday")
    {
        var (result, albumId) = await Service.CreateAlbumAsync(userId, name);
        Assert.True(result.Succeeded, result.Message);
        return albumId!.Value;
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

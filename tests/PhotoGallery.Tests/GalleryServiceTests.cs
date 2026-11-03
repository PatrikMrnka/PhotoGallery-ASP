using Microsoft.EntityFrameworkCore;
using PhotoGallery.Tests.Infrastructure;
using PhotoGallery.Web.Options;
using PhotoGallery.Web.Services;
using static PhotoGallery.Tests.Infrastructure.GalleryTestContext;

namespace PhotoGallery.Tests;

public class GalleryServiceTests
{
    [Fact]
    public async Task Upload_stores_file_thumbnail_and_adds_photo_to_default_album()
    {
        await using var ctx = await CreateAsync();

        var photoId = await ctx.UploadAsync(Alice);

        Assert.True(ctx.Storage.Files.ContainsKey(photoId));
        Assert.True(await ctx.Db.Thumbnails.AnyAsync(t => t.PhotoId == photoId));

        var defaultAlbum = await ctx.Service.GetOrCreateDefaultAlbumAsync(Alice);
        Assert.True(await ctx.Db.AlbumPhotos.AnyAsync(ap => ap.AlbumId == defaultAlbum.Id && ap.PhotoId == photoId));
    }

    [Fact]
    public async Task Upload_into_album_adds_photo_to_that_album_and_default_album()
    {
        await using var ctx = await CreateAsync();
        var albumId = await ctx.CreateAlbumAsync(Alice);

        var photoId = await ctx.UploadAsync(Alice, albumId: albumId);

        var albums = await ctx.Db.AlbumPhotos.Where(ap => ap.PhotoId == photoId).CountAsync();
        Assert.Equal(2, albums);
    }

    [Fact]
    public async Task Upload_rejects_files_that_are_not_images()
    {
        await using var ctx = await CreateAsync();
        var bytes = "this is not an image"u8.ToArray();

        var result = await ctx.Service.UploadAsync(
            Alice, [new UploadFile("evil.png", bytes.Length, new MemoryStream(bytes))], isPublic: true, albumId: null);

        Assert.Empty(result.PhotoIds);
        Assert.Contains("evil.png", Assert.Single(result.Errors));
        Assert.Empty(ctx.Storage.Files);
    }

    [Fact]
    public async Task Upload_into_someone_elses_album_is_rejected()
    {
        await using var ctx = await CreateAsync();
        var bobsAlbum = await ctx.CreateAlbumAsync(Bob);
        var data = TestImages.Png();

        var result = await ctx.Service.UploadAsync(
            Alice, [new UploadFile("a.png", data.Length, new MemoryStream(data))], isPublic: false, albumId: bobsAlbum);

        Assert.Empty(result.PhotoIds);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Upload_respects_the_per_user_photo_limit()
    {
        await using var ctx = await CreateAsync(new UploadOptions { MaxPhotosPerUser = 2 });
        var data = TestImages.Png();
        UploadFile Upload(string name) => new(name, data.Length, new MemoryStream(data));

        var result = await ctx.Service.UploadAsync(Alice, [Upload("1.png"), Upload("2.png"), Upload("3.png")], false, null);

        Assert.Equal(2, result.PhotoIds.Count);
        Assert.Contains("3.png", Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Private_photo_is_visible_only_to_its_owner()
    {
        await using var ctx = await CreateAsync();
        var photoId = await ctx.UploadAsync(Alice, isPublic: false);

        Assert.NotNull(await ctx.Service.GetPhotoAsync(photoId, Alice));
        Assert.Null(await ctx.Service.GetPhotoAsync(photoId, Bob));
        Assert.Null(await ctx.Service.GetPhotoAsync(photoId, null));

        Assert.Null(await ctx.Service.OpenOriginalAsync(photoId, Bob));
        Assert.Null(await ctx.Service.GetThumbnailAsync(photoId, null));
    }

    [Fact]
    public async Task Public_photo_is_visible_to_everyone()
    {
        await using var ctx = await CreateAsync();
        var photoId = await ctx.UploadAsync(Alice, isPublic: true);

        var photo = await ctx.Service.GetPhotoAsync(photoId, null);
        Assert.NotNull(photo);
        Assert.False(photo.IsOwner);

        var original = await ctx.Service.OpenOriginalAsync(photoId, Bob);
        Assert.NotNull(original);
        await original.Content.DisposeAsync();
        Assert.NotNull(await ctx.Service.GetThumbnailAsync(photoId, null));
    }

    [Fact]
    public async Task Public_photos_listing_contains_only_public_photos()
    {
        await using var ctx = await CreateAsync();
        var publicId = await ctx.UploadAsync(Alice, isPublic: true);
        await ctx.UploadAsync(Alice, isPublic: false);

        var page = await ctx.Service.GetPublicPhotosAsync(page: 1, pageSize: 10);

        Assert.Equal(publicId, Assert.Single(page.Items).Id);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task Private_album_is_hidden_from_other_users()
    {
        await using var ctx = await CreateAsync();
        var albumId = await ctx.CreateAlbumAsync(Alice);

        Assert.NotNull(await ctx.Service.GetAlbumAsync(albumId, Alice, SortOrder.Newest));
        Assert.Null(await ctx.Service.GetAlbumAsync(albumId, Bob, SortOrder.Newest));
        Assert.Null(await ctx.Service.GetAlbumAsync(albumId, null, SortOrder.Newest));
    }

    [Fact]
    public async Task Public_album_hides_the_owners_private_photos_from_visitors()
    {
        await using var ctx = await CreateAsync();
        var albumId = await ctx.CreateAlbumAsync(Alice);
        await ctx.Service.SetAlbumVisibilityAsync(albumId, Alice, isPublic: true);
        var privatePhoto = await ctx.UploadAsync(Alice, isPublic: false, albumId: albumId);
        var publicPhoto = await ctx.UploadAsync(Alice, isPublic: true, albumId: albumId);

        var asVisitor = await ctx.Service.GetAlbumAsync(albumId, null, SortOrder.Newest);
        var asOwner = await ctx.Service.GetAlbumAsync(albumId, Alice, SortOrder.Newest);

        Assert.NotNull(asVisitor);
        Assert.Equal(publicPhoto, Assert.Single(asVisitor.Photos).Id);
        Assert.Equal(1, asVisitor.Album.PhotoCount);
        Assert.NotNull(asOwner);
        Assert.Contains(asOwner.Photos, p => p.Id == privatePhoto);
        Assert.True(asOwner.IsOwner);
    }

    [Fact]
    public async Task Publishing_an_album_publishes_the_owners_photos_in_it()
    {
        await using var ctx = await CreateAsync();
        var albumId = await ctx.CreateAlbumAsync(Alice);
        var photoId = await ctx.UploadAsync(Alice, isPublic: false, albumId: albumId);
        var otherPhoto = await ctx.UploadAsync(Alice, isPublic: false);

        var result = await ctx.Service.SetAlbumVisibilityAsync(albumId, Alice, isPublic: true);

        Assert.True(result.Succeeded);
        Assert.True((await ctx.Db.Photos.SingleAsync(p => p.Id == photoId)).IsPublic);
        Assert.False((await ctx.Db.Photos.SingleAsync(p => p.Id == otherPhoto)).IsPublic);
    }

    [Fact]
    public async Task Only_the_owner_can_change_or_delete_a_photo()
    {
        await using var ctx = await CreateAsync();
        var publicPhoto = await ctx.UploadAsync(Alice, isPublic: true);
        var privatePhoto = await ctx.UploadAsync(Alice, isPublic: false);

        Assert.Equal(OperationStatus.Forbidden, (await ctx.Service.DeletePhotoAsync(publicPhoto, Bob)).Status);
        Assert.Equal(OperationStatus.Forbidden, (await ctx.Service.SetPhotoVisibilityAsync(publicPhoto, Bob, false)).Status);

        // A hidden photo must look exactly like a missing one.
        Assert.Equal(OperationStatus.NotFound, (await ctx.Service.DeletePhotoAsync(privatePhoto, Bob)).Status);

        Assert.Equal(2, await ctx.Db.Photos.CountAsync());
    }

    [Fact]
    public async Task Deleting_a_photo_removes_the_file_and_album_links()
    {
        await using var ctx = await CreateAsync();
        var albumId = await ctx.CreateAlbumAsync(Alice);
        var photoId = await ctx.UploadAsync(Alice, albumId: albumId);

        var result = await ctx.Service.DeletePhotoAsync(photoId, Alice);

        Assert.True(result.Succeeded);
        Assert.False(ctx.Storage.Files.ContainsKey(photoId));
        Assert.False(await ctx.Db.AlbumPhotos.AnyAsync(ap => ap.PhotoId == photoId));
        Assert.False(await ctx.Db.Thumbnails.AnyAsync(t => t.PhotoId == photoId));
    }

    [Fact]
    public async Task Default_album_cannot_be_renamed_published_or_deleted()
    {
        await using var ctx = await CreateAsync();
        var defaultAlbum = await ctx.Service.GetOrCreateDefaultAlbumAsync(Alice);

        Assert.Equal(OperationStatus.Invalid, (await ctx.Service.RenameAlbumAsync(defaultAlbum.Id, Alice, "New")).Status);
        Assert.Equal(OperationStatus.Invalid, (await ctx.Service.SetAlbumVisibilityAsync(defaultAlbum.Id, Alice, true)).Status);
        Assert.Equal(OperationStatus.Invalid, (await ctx.Service.DeleteAlbumAsync(defaultAlbum.Id, Alice)).Status);
    }

    [Fact]
    public async Task Default_album_is_created_only_once()
    {
        await using var ctx = await CreateAsync();

        var first = await ctx.Service.GetOrCreateDefaultAlbumAsync(Alice);
        var second = await ctx.Service.GetOrCreateDefaultAlbumAsync(Alice);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await ctx.Db.Albums.CountAsync(a => a.OwnerId == Alice && a.IsDefault));
    }

    [Fact]
    public async Task Deleting_an_album_keeps_its_photos()
    {
        await using var ctx = await CreateAsync();
        var albumId = await ctx.CreateAlbumAsync(Alice);
        var photoId = await ctx.UploadAsync(Alice, albumId: albumId);

        var result = await ctx.Service.DeleteAlbumAsync(albumId, Alice);

        Assert.True(result.Succeeded);
        Assert.True(await ctx.Db.Photos.AnyAsync(p => p.Id == photoId));
        Assert.False(await ctx.Db.Albums.AnyAsync(a => a.Id == albumId));
    }

    [Fact]
    public async Task Users_can_add_public_photos_of_others_but_not_private_ones()
    {
        await using var ctx = await CreateAsync();
        var bobsAlbum = await ctx.CreateAlbumAsync(Bob);
        var alicesPublic = await ctx.UploadAsync(Alice, isPublic: true);
        var alicesPrivate = await ctx.UploadAsync(Alice, isPublic: false);

        Assert.True((await ctx.Service.AddPhotoToAlbumAsync(alicesPublic, bobsAlbum, Bob)).Succeeded);
        Assert.Equal(OperationStatus.NotFound, (await ctx.Service.AddPhotoToAlbumAsync(alicesPrivate, bobsAlbum, Bob)).Status);
        Assert.Equal(OperationStatus.Invalid, (await ctx.Service.AddPhotoToAlbumAsync(alicesPublic, bobsAlbum, Bob)).Status);
    }

    [Fact]
    public async Task Users_cannot_add_photos_to_albums_of_others()
    {
        await using var ctx = await CreateAsync();
        var alicesAlbum = await ctx.CreateAlbumAsync(Alice);
        var bobsPhoto = await ctx.UploadAsync(Bob, isPublic: true);

        var result = await ctx.Service.AddPhotoToAlbumAsync(bobsPhoto, alicesAlbum, Bob);

        Assert.Equal(OperationStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Photos_cannot_be_removed_from_the_default_album()
    {
        await using var ctx = await CreateAsync();
        var photoId = await ctx.UploadAsync(Alice);
        var defaultAlbum = await ctx.Service.GetOrCreateDefaultAlbumAsync(Alice);

        var result = await ctx.Service.RemovePhotoFromAlbumAsync(photoId, defaultAlbum.Id, Alice);

        Assert.Equal(OperationStatus.Invalid, result.Status);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Holiday", true)]
    public async Task Album_name_is_validated(string name, bool valid)
    {
        await using var ctx = await CreateAsync();

        var (result, _) = await ctx.Service.CreateAlbumAsync(Alice, name);

        Assert.Equal(valid, result.Succeeded);
    }

    [Fact]
    public async Task Album_name_longer_than_limit_is_rejected()
    {
        await using var ctx = await CreateAsync();

        var (result, _) = await ctx.Service.CreateAlbumAsync(Alice, new string('a', 101));

        Assert.Equal(OperationStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Orphaned_files_are_deleted()
    {
        await using var ctx = await CreateAsync();
        var photoId = await ctx.UploadAsync(Alice);
        var orphan = Guid.NewGuid();
        await ctx.Storage.SaveAsync(orphan, [1, 2, 3]);

        var deleted = await ctx.Service.DeleteOrphanedFilesAsync();

        Assert.Equal(1, deleted);
        Assert.True(ctx.Storage.Files.ContainsKey(photoId));
        Assert.False(ctx.Storage.Files.ContainsKey(orphan));
    }

    [Theory]
    [InlineData(@"C:\Users\me\Pictures\cat.jpg", "cat.jpg")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("", "photo")]
    [InlineData(null, "photo")]
    public void File_names_are_sanitized(string? input, string expected)
    {
        Assert.Equal(expected, GalleryService.SanitizeFileName(input));
    }
}

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using PhotoGallery.Tests.Infrastructure;

namespace PhotoGallery.Tests;

/// <summary>End-to-end checks through HTTP, including the original security issues of the school version.</summary>
public class WebApplicationTests(GalleryWebApplicationFactory factory) : IClassFixture<GalleryWebApplicationFactory>
{
    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Theory]
    [InlineData("/")]
    [InlineData("/Identity/Account/Login")]
    [InlineData("/Identity/Account/Register")]
    [InlineData("/healthz")]
    public async Task Public_pages_load(string url)
    {
        var response = await CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Albums")]
    [InlineData("/Photos/Upload")]
    public async Task Private_pages_require_sign_in(string url)
    {
        var response = await CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Identity/Account/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Public_photo_is_served_to_anonymous_visitors()
    {
        var owner = await factory.CreateUserAsync($"{Guid.NewGuid():N}@example.com");
        var photoId = await factory.UploadAsync(owner, isPublic: true);
        var client = CreateClient();

        var original = await client.GetAsync($"/media/{photoId}");
        var thumbnail = await client.GetAsync($"/media/{photoId}/thumb");
        var page = await client.GetAsync($"/Photos/Details/{photoId}");

        Assert.Equal(HttpStatusCode.OK, original.StatusCode);
        Assert.Equal("image/png", original.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.OK, thumbnail.StatusCode);
        Assert.Equal("image/webp", thumbnail.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
    }

    [Fact]
    public async Task Private_photo_is_not_served_to_anonymous_visitors()
    {
        var owner = await factory.CreateUserAsync($"{Guid.NewGuid():N}@example.com");
        var photoId = await factory.UploadAsync(owner, isPublic: false);
        var client = CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/media/{photoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/media/{photoId}/thumb")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Photos/Details/{photoId}")).StatusCode);
    }

    [Fact]
    public async Task Private_album_is_not_shown_to_anonymous_visitors()
    {
        var owner = await factory.CreateUserAsync($"{Guid.NewGuid():N}@example.com");
        var privateAlbum = await factory.CreateAlbumAsync(owner, isPublic: false);
        var publicAlbum = await factory.CreateAlbumAsync(owner, isPublic: true);
        var client = CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Albums/Details/{privateAlbum}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Albums/Details/{publicAlbum}")).StatusCode);
    }

    [Fact]
    public async Task Unknown_photo_returns_not_found()
    {
        var response = await CreateClient().GetAsync($"/media/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Changing_data_without_antiforgery_token_is_rejected()
    {
        var response = await CreateClient().PostAsync(
            $"/Albums/Details/{Guid.NewGuid()}?handler=Delete", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

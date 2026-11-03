using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoGallery.Web.Data;
using PhotoGallery.Web.Endpoints;
using PhotoGallery.Web.Models;
using PhotoGallery.Web.Options;
using PhotoGallery.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));
builder.Services.Configure<DemoOptions>(builder.Configuration.GetSection(DemoOptions.SectionName));

// Database: SQLite file, relative paths are resolved against the content root.
var connectionString = ResolveSqlitePath(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=App_Data/photogallery.db",
    builder.Environment.ContentRootPath);
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        // No e-mail service is configured, so accounts are usable right after registration.
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

// Keep sign-in cookies valid across restarts when App_Data is on a persistent disk.
builder.Services.AddDataProtection()
    .SetApplicationName("PhotoGallery")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPhotoStorage>(sp =>
{
    var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    return new FileSystemPhotoStorage(Path.Combine(environment.ContentRootPath, options.Path));
});
builder.Services.AddSingleton<IImageProcessor, ImageSharpProcessor>();
builder.Services.AddScoped<GalleryService>();

// Hosting platforms terminate TLS in a reverse proxy and forward the original scheme.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// The shared demo account must stay usable for every visitor.
app.Use(async (context, next) =>
{
    var demo = context.RequestServices.GetRequiredService<IOptions<DemoOptions>>().Value;
    if (demo.Enabled
        && context.Request.Path.StartsWithSegments("/Identity/Account/Manage")
        && string.Equals(context.User.Identity?.Name, demo.Email, StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/DemoAccount");
        return;
    }

    await next();
});

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapMediaEndpoints();
app.MapGet("/healthz", () => Results.Ok("ok"));

await DatabaseInitializer.InitializeAsync(app.Services);

app.Run();

static string ResolveSqlitePath(string connectionString, string contentRoot)
{
    var builder = new SqliteConnectionStringBuilder(connectionString);
    if (builder.DataSource != ":memory:" && !Path.IsPathRooted(builder.DataSource))
    {
        builder.DataSource = Path.Combine(contentRoot, builder.DataSource);
    }

    var directory = Path.GetDirectoryName(builder.DataSource);
    if (!string.IsNullOrEmpty(directory))
    {
        Directory.CreateDirectory(directory);
    }

    return builder.ToString();
}

/// <summary>Exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program
{
}

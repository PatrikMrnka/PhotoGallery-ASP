namespace PhotoGallery.Web.Options;

/// <summary>Where the original photo files are stored on disk.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Directory for uploaded files. Relative paths are resolved against the content root.</summary>
    public string Path { get; set; } = "App_Data/uploads";
}

/// <summary>Limits applied to uploaded images.</summary>
public sealed class UploadOptions
{
    public const string SectionName = "Upload";

    /// <summary>Maximum size of a single file in bytes.</summary>
    public long MaxFileBytes { get; set; } = 15 * 1024 * 1024;

    /// <summary>Maximum number of files in one upload request.</summary>
    public int MaxFilesPerRequest { get; set; } = 20;

    /// <summary>Maximum number of photos a single user may own. 0 means unlimited.</summary>
    public int MaxPhotosPerUser { get; set; }

    /// <summary>Maximum image resolution (width × height). Protects against decompression bombs.</summary>
    public long MaxPixels { get; set; } = 40_000_000;

    /// <summary>Longest side of a generated thumbnail in pixels.</summary>
    public int ThumbnailSize { get; set; } = 480;
}

/// <summary>Settings for the public live demo.</summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    /// <summary>Seeds a shared demo account with sample photos and shows a demo banner.</summary>
    public bool Enabled { get; set; }

    /// <summary>Deletes the database and all uploads on startup, so every restart begins clean.</summary>
    public bool ResetOnStartup { get; set; }

    public string Email { get; set; } = "demo@photogallery.local";

    public string Password { get; set; } = "Demo1234";
}

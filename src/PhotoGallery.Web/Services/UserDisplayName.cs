namespace PhotoGallery.Web.Services;

public static class UserDisplayName
{
    /// <summary>
    /// User names are e-mail addresses. Only the part before "@" is shown publicly,
    /// so visitors cannot harvest full addresses from the gallery.
    /// </summary>
    public static string From(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return "unknown";
        }

        var at = userName.IndexOf('@');
        return at > 0 ? userName[..at] : userName;
    }
}

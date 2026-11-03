using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PhotoGallery.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? Code { get; set; }

    public string? RequestId { get; private set; }

    public bool IsNotFound => Code == StatusCodes.Status404NotFound;

    public void OnGet() => RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    public void OnPost() => OnGet();
}

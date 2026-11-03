using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PhotoGallery.Web.Services;

namespace PhotoGallery.Web.Pages;

/// <summary>Shared helpers for gallery pages: current user, status messages and result handling.</summary>
public abstract class GalleryPageModel : PageModel
{
    public const string SuccessKey = "StatusSuccess";
    public const string ErrorKey = "StatusError";

    protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    protected string RequiredUserId =>
        CurrentUserId ?? throw new InvalidOperationException("This page requires a signed-in user.");

    protected void ShowSuccess(string message) => TempData[SuccessKey] = message;

    protected void ShowError(string message) => TempData[ErrorKey] = message;

    /// <summary>Runs a command for the signed-in user and turns its result into a response.</summary>
    protected async Task<IActionResult> RunAsync(
        Func<string, Task<OperationResult>> command,
        Func<IActionResult> onCompleted)
    {
        if (CurrentUserId is not { } userId)
        {
            return Challenge();
        }

        var result = await command(userId);
        switch (result.Status)
        {
            case OperationStatus.NotFound:
                return NotFound();
            case OperationStatus.Forbidden:
                return Forbid();
            case OperationStatus.Ok:
                ShowSuccess(result.Message);
                break;
            default:
                ShowError(result.Message);
                break;
        }

        return onCompleted();
    }
}

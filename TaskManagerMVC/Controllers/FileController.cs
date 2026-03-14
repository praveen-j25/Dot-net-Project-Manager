using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagerMVC.Services;

namespace TaskManagerMVC.Controllers;

[Authorize]
public class FileController : Controller
{
    private readonly TaskService _taskService;
    private readonly UserService _userService;

    public FileController(TaskService taskService, UserService userService)
    {
        _taskService = taskService;
        _userService = userService;
    }

    /// <summary>
    /// Secure download endpoint for task attachments.
    /// Only authenticated users can access.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Attachment(int id)
    {
        var result = await _taskService.DownloadAttachmentAsync(id);
        if (result == null || result.Value.Content == null)
            return NotFound("Attachment not found.");

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>
    /// Secure endpoint for serving profile images.
    /// Only authenticated users can access.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ProfileImage(int id)
    {
        var result = await _userService.GetProfileImageContentAsync(id);
        if (result == null || result.Value.Content == null)
            return NotFound("Profile image not found.");

        // Cache for 5 minutes since profile images don't change often
        Response.Headers["Cache-Control"] = "private, max-age=300";
        return File(result.Value.Content, result.Value.ContentType);
    }
}

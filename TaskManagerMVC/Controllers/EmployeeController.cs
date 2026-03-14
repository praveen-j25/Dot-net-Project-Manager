using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagerMVC.Services;
using TaskManagerMVC.ViewModels;

namespace TaskManagerMVC.Controllers;

[Authorize(Policy = "EmployeeOnly")]
public class EmployeeController : Controller
{
    private readonly DashboardService _dashboardService;
    private readonly UserService _userService;
    private readonly TaskService _taskService;
    private readonly CommentService _commentService;
    private readonly NotificationService _notificationService;
    private readonly ActivityLogService _activityLogService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public EmployeeController(
        DashboardService dashboardService,
        UserService userService,
        TaskService taskService,
        CommentService commentService,
        NotificationService notificationService,
        ActivityLogService activityLogService,
        IWebHostEnvironment webHostEnvironment)
    {
        _dashboardService = dashboardService;
        _userService = userService;
        _taskService = taskService;
        _commentService = commentService;
        _notificationService = notificationService;
        _activityLogService = activityLogService;
        _webHostEnvironment = webHostEnvironment;
        _notificationService = notificationService;
        _activityLogService = activityLogService;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string UserName => User.FindFirstValue(ClaimTypes.Name) ?? "Employee";

    // ============================================
    // DASHBOARD
    // ============================================

    public async Task<IActionResult> Index()
    {
        var model = await _dashboardService.GetEmployeeDashboardAsync(UserId, UserName);
        model.Notifications = await _notificationService.GetUserNotificationsAsync(UserId, 5, true);
        model.RecentActivity = await _activityLogService.GetRecentActivityAsync(UserId, 10);
        return View(model);
    }

    // ============================================
    // MY TASKS
    // ============================================

    public async Task<IActionResult> MyTasks(int? statusId, int? priorityId, string? sortBy)
    {
        var model = await _taskService.GetEmployeeTasksAsync(UserId, statusId, priorityId, sortBy);
        return View(model);
    }

    // ============================================
    // TASK DETAILS & RESPONSE
    // ============================================

    public async Task<IActionResult> TaskDetails(int id)
    {
        var model = await _taskService.GetTaskDetailsEnhancedAsync(id);
        if (model == null)
        {
            TempData["Error"] = $"Task with ID {id} not found. It may have been deleted.";
            return RedirectToAction(nameof(MyTasks));
        }
        
        // Verify this task is assigned to the current user or they created it
        // Note: CreatedById of 0 means system-created, only allow if assigned to user
        bool isAssignedToUser = model.AssignedTo == UserId;
        bool isCreatedByUser = model.CreatedById > 0 && model.CreatedById == UserId;
        
        if (!isAssignedToUser && !isCreatedByUser)
        {
            TempData["Error"] = "You don't have permission to view this task.";
            return RedirectToAction(nameof(MyTasks));
        }

        // Load statuses for response form
        model.Response = new TaskResponseVM
        {
            TaskId = id,
            Statuses = await _taskService.GetStatusSelectListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitResponse(TaskResponseVM model)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Comment))
        {
            TempData["Error"] = "Please enter a comment";
            return RedirectToAction(nameof(TaskDetails), new { id = model.TaskId });
        }

        // Verify task ownership
        var task = await _taskService.GetTaskByIdAsync(model.TaskId);
        if (task == null) return NotFound();

        await _commentService.AddTaskResponseAsync(model.TaskId, UserId, model);

        // Log activity
        var action = model.StatusId.HasValue ? "Updated status" : "Added comment";
        await _activityLogService.LogActivityAsync(model.TaskId, UserId, action);

        // Notify task creator/assigner (only if creator exists and is different)
        if (task.CreatedById > 0 && task.CreatedById != UserId)
        {
            await _notificationService.NotifyTaskUpdatedAsync(model.TaskId, task.CreatedById, task.Title!, "Employee response added");
        }

        TempData["Success"] = "Response submitted successfully!";
        return RedirectToAction(nameof(TaskDetails), new { id = model.TaskId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTaskStatus(int taskId, int statusId)
    {
        var task = await _taskService.GetTaskByIdAsync(taskId);
        if (task == null) return NotFound();

        await _taskService.UpdateTaskStatusAsync(taskId, statusId, UserId);
        
        // Notify task creator/assigner (only if creator exists and is different)
        if (task.CreatedById > 0 && task.CreatedById != UserId)
        {
            await _notificationService.NotifyTaskUpdatedAsync(taskId, task.CreatedById, task.Title!, $"Status changed");
        }
        else 
        {
            // If user updated their own task, notify admins? 
            // For now, let's just stick to notifying the creator if it's different.
        }

        TempData["Success"] = "Status updated successfully!";
        return RedirectToAction(nameof(MyTasks));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTaskProgress(int taskId, int progress)
    {
        var task = await _taskService.GetTaskByIdAsync(taskId);
        if (task == null) return NotFound();

        await _taskService.UpdateTaskProgressAsync(taskId, progress, UserId);
        
        // Notify task creator/assigner (only if creator exists and is different)
        if (task.CreatedById > 0 && task.CreatedById != UserId)
        {
            await _notificationService.NotifyTaskUpdatedAsync(taskId, task.CreatedById, task.Title!, $"Progress updated to {progress}%");
        }

        TempData["Success"] = "Progress updated successfully!";
        return RedirectToAction(nameof(TaskDetails), new { id = taskId });
    }

    // ============================================
    // TIME LOGGING
    // ============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogTime(int taskId, decimal hours, string? description, bool isBillable = false)
    {
        if (hours < 0.25m || hours > 24)
        {
            TempData["Error"] = "Please enter valid hours (0.25-24)";
            return RedirectToAction(nameof(TaskDetails), new { id = taskId });
        }

        Console.WriteLine($"[DEBUG] LogTime called for TaskId: {taskId}, Hours: {hours}, IsBillable: {isBillable}");
        await _commentService.LogTimeAsync(taskId, UserId, hours, description, isBillable);
        
        TempData["Success"] = $"{hours} hours logged successfully!";
        return RedirectToAction(nameof(TaskDetails), new { id = taskId });
    }

    public async Task<IActionResult> MyTimeLogs(DateTime? startDate, DateTime? endDate)
    {
        startDate ??= DateTime.Today.AddDays(-30);
        endDate ??= DateTime.Today;

        var logs = await _taskService.GetUserTimeLogsAsync(UserId, startDate.Value, endDate.Value);
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;
        return View(logs);
    }

    // ============================================
    // COMMENTS
    // ============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int taskId, string comment, bool isInternal = false)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            TempData["Error"] = "Please enter a comment";
            return RedirectToAction(nameof(TaskDetails), new { id = taskId });
        }

        await _commentService.AddCommentAsync(taskId, UserId, comment, "comment", isInternal);
        
        // Notify relevant parties (only if creator exists and is different)
        var task = await _taskService.GetTaskByIdAsync(taskId);
        if (task != null && task.CreatedById > 0 && task.CreatedById != UserId)
        {
            await _notificationService.NotifyCommentAddedAsync(taskId, task.CreatedById, task.Title!, UserName);
        }

        TempData["Success"] = "Comment added successfully!";
        return RedirectToAction(nameof(TaskDetails), new { id = taskId });
    }

    // ============================================
    // NOTIFICATIONS
    // ============================================

    public async Task<IActionResult> Notifications()
    {
        var notifications = await _notificationService.GetUserNotificationsAsync(UserId);
        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        await _notificationService.MarkAsReadAsync(id, UserId);
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        await _notificationService.MarkAllAsReadAsync(UserId);
        return RedirectToAction(nameof(Notifications));
    }

    [HttpGet]
    public async Task<IActionResult> GetRecentNotifications()
    {
        var notifications = await _notificationService.GetUserNotificationsAsync(UserId, 5);
        return Json(notifications);
    }

    // ============================================
    // PROFILE & SETTINGS
    // ============================================

    public async Task<IActionResult> Profile()
    {
        var model = await _userService.GetProfileAsync(UserId);
        return View(model);
    }

    [HttpGet]
    public IActionResult Settings()
    {
        var model = new SettingsVM
        {
            UserId = UserId,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? ""
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(SettingsVM model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Use the secure password change method that verifies current password
            var (success, message) = await _userService.ChangePasswordAsync(UserId, model.CurrentPassword, model.NewPassword);
            
            if (!success)
            {
                TempData["Error"] = message;
                return View(model);
            }
            
            TempData["Success"] = "Password updated successfully!";
            return RedirectToAction(nameof(Settings));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to update settings: {ex.Message}";
            return View(model);
        }
    }

    // ============================================
    // API ENDPOINTS
    // ============================================

    [HttpGet]
    public async Task<IActionResult> GetUnreadNotificationCount()
    {
        var count = await _notificationService.GetUnreadCountAsync(UserId);
        return Json(new { count });
    }

    [HttpGet]
    public async Task<IActionResult> GetTaskComments(int taskId)
    {
        var comments = await _commentService.GetTaskCommentsAsync(taskId);
        return Json(comments);
    }

    // ============================================
    // PROFILE PICTURE MANAGEMENT
    // ============================================

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var model = await _userService.GetProfileForEditAsync(UserId);
        if (model == null)
            return NotFound();
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(ProfileEditVM model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.Id = UserId; // Ensure user can only update their own profile
        var (success, message) = await _userService.UpdateProfileAsync(model);

        if (success)
        {
            TempData["Success"] = message;
            return RedirectToAction(nameof(Profile));
        }

        TempData["Error"] = message;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UploadProfilePicture(IFormFile profileImage)
    {
        var (success, message, imagePath) = await _userService.UpdateProfilePictureAsync(
            UserId, 
            profileImage
        );

        return Json(new { success, message, imagePath });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveProfilePicture()
    {
        var (success, message) = await _userService.RemoveProfilePictureAsync(
            UserId
        );

        return Json(new { success, message });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagerMVC.Services;
using TaskManagerMVC.ViewModels;

namespace TaskManagerMVC.Controllers;

[Authorize(Policy = "ManagerOnly")]
public class ManagerController : Controller
{
    private readonly ManagerService _managerService;
    private readonly UserService _userService;
    private readonly ProjectService _projectService;
    private readonly TaskService _taskService;
    private readonly NotificationService _notificationService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ManagerController(
        ManagerService managerService,
        UserService userService,
        ProjectService projectService,
        TaskService taskService,
        NotificationService notificationService,
        IWebHostEnvironment webHostEnvironment)
    {
        _managerService = managerService;
        _userService = userService;
        _projectService = projectService;
        _taskService = taskService;
        _notificationService = notificationService;
        _webHostEnvironment = webHostEnvironment;
    }

    private int ManagerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string ManagerName => User.FindFirstValue(ClaimTypes.Name) ?? "Manager";

    // ============================================
    // DASHBOARD
    // ============================================

    public async Task<IActionResult> Index()
    {
        var model = await _managerService.GetManagerDashboardAsync(ManagerId);
        return View(model);
    }

    // ============================================
    // MY TEAM
    // ============================================

    [HttpGet]
    [Route("Manager/MyTeam")]
    public async Task<IActionResult> MyTeam()
    {
        var model = await _managerService.GetTeamMembersAsync(ManagerId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> TeamMemberDetails(int id)
    {
        // Security check: Verify the team member belongs to manager's department
        var teamMembers = await _managerService.GetTeamMembersAsync(ManagerId);
        var member = teamMembers.Members.FirstOrDefault(m => m.Id == id);
        
        if (member == null)
        {
            TempData["Error"] = "You don't have permission to view this team member.";
            return RedirectToAction(nameof(MyTeam));
        }
        
        // Get team member profile details
        var profile = await _userService.GetProfileAsync(id);
        if (profile == null)
        {
            TempData["Error"] = "Team member not found.";
            return RedirectToAction(nameof(MyTeam));
        }
        
        return View("TeamMemberProfile", profile);
    }

    // ============================================
    // MY PROJECTS
    // ============================================

    public async Task<IActionResult> MyProjects(string? status)
    {
        var model = await _managerService.GetManagedProjectsAsync(ManagerId, status);
        return View(model);
    }

    public async Task<IActionResult> ProjectDetails(int id)
    {
        var model = await _projectService.GetProjectDetailsAsync(id);
        
        // Security check: Ensure manager owns this project
        if (model == null || model.ManagerId != ManagerId)
        {
            TempData["Error"] = "You don't have permission to view this project.";
            return RedirectToAction(nameof(MyProjects));
        }
        
        return View(model);
    }

    // ============================================
    // TASK MANAGEMENT
    // ============================================

    public async Task<IActionResult> TaskManagement(int? statusId, int? priorityId, int? projectId)
    {
        // Use manager-specific method that filters by manager's projects
        var model = await _taskService.GetTasksForManagerAsync(ManagerId, statusId, priorityId, projectId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AssignTask()
    {
        var model = await _taskService.GetTaskAssignmentFormAsync(null, ManagerId);
        
        // Filter projects to only show manager's projects
        // TODO: Add filtering in service layer
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTask(TaskAssignmentVM model)
    {
        // Validate file sizes (10MB max per file)
        if (model.Attachments != null && model.Attachments.Any())
        {
            const long maxFileSize = 17 * 1024 * 1024; // 10MB
            var oversizedFiles = model.Attachments.Where(f => f.Length > maxFileSize).ToList();
            
            if (oversizedFiles.Any())
            {
                var fileNames = string.Join(", ", oversizedFiles.Select(f => f.FileName));
                var errorMessage = $"The following files exceed the 10MB limit: {fileNames}";
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = errorMessage });
                }
                
                ModelState.AddModelError("Attachments", errorMessage);
            }
        }
        
        if (!ModelState.IsValid)
        {
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Please fill in all required fields." });
            }
            
            var vm = await _taskService.GetTaskAssignmentFormAsync(null, ManagerId);
            model.Projects = vm.Projects;
            model.Categories = vm.Categories;
            model.Priorities = vm.Priorities;
            model.Statuses = vm.Statuses;
            model.Employees = vm.Employees;
            model.TaskTypes = vm.TaskTypes;
            return View(model);
        }

        try
        {
            var taskId = await _taskService.AssignTaskAsync(ManagerId, model);

            // Handle file uploads
            Console.WriteLine($"[DEBUG] ManagerController: Attachments count: {model.Attachments?.Count ?? 0}");
            if (model.Attachments != null && model.Attachments.Any())
            {
                await _taskService.SaveAttachmentsAsync(taskId, ManagerId, model.Attachments);
            }
            
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Task assigned successfully!" });
            }
            
            TempData["Success"] = "Task assigned successfully!";
            return RedirectToAction(nameof(TaskManagement));
        }
        catch (Exception ex)
        {
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
            
            TempData["Error"] = $"Failed to assign task: {ex.Message}";
            var vm = await _taskService.GetTaskAssignmentFormAsync(null, ManagerId);
            model.Projects = vm.Projects;
            model.Categories = vm.Categories;
            model.Priorities = vm.Priorities;
            model.Statuses = vm.Statuses;
            model.Employees = vm.Employees;
            model.TaskTypes = vm.TaskTypes;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditTask(int id)
    {
        var model = await _taskService.GetTaskAssignmentFormAsync(id, ManagerId);
        if (model.Id == 0) return NotFound();
        
        // Security check: Ensure task belongs to manager's project
        var task = await _taskService.GetTaskDetailsEnhancedAsync(id);
        if (task == null)
        {
            TempData["Error"] = "Task not found.";
            return RedirectToAction(nameof(TaskManagement));
        }
        
        // Verify the task's project is managed by this manager
        if (!task.ProjectId.HasValue)
        {
            TempData["Error"] = "Task is not assigned to a project.";
            return RedirectToAction(nameof(TaskManagement));
        }
        
        var project = await _projectService.GetProjectDetailsAsync(task.ProjectId.Value);
        if (project == null || project.ManagerId != ManagerId)
        {
            TempData["Error"] = "You don't have permission to edit this task.";
            return RedirectToAction(nameof(TaskManagement));
        }
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTask(int id, TaskAssignmentVM model)
    {
        if (id != model.Id) return BadRequest();

        // Security check: Ensure task belongs to manager's project
        var task = await _taskService.GetTaskDetailsEnhancedAsync(id);
        if (task == null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Task not found." });
            }
            TempData["Error"] = "Task not found.";
            return RedirectToAction(nameof(TaskManagement));
        }
        
        // Verify the task's project is managed by this manager
        if (!task.ProjectId.HasValue)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Task is not assigned to a project." });
            }
            TempData["Error"] = "Task is not assigned to a project.";
            return RedirectToAction(nameof(TaskManagement));
        }
        
        var project = await _projectService.GetProjectDetailsAsync(task.ProjectId.Value);
        if (project == null || project.ManagerId != ManagerId)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "You don't have permission to edit this task." });
            }
            TempData["Error"] = "You don't have permission to edit this task.";
            return RedirectToAction(nameof(TaskManagement));
        }

        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                                    .SelectMany(x => x.Errors)
                                    .Select(x => x.ErrorMessage));
            Console.WriteLine($"[DEBUG] ModelState Invalid: {errors}");

            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = $"Validation failed: {errors}" });
            }
            
            var vm = await _taskService.GetTaskAssignmentFormAsync(null, ManagerId);
            model.Projects = vm.Projects;
            model.Categories = vm.Categories;
            model.Priorities = vm.Priorities;
            model.Statuses = vm.Statuses;
            model.Employees = vm.Employees;
            model.TaskTypes = vm.TaskTypes;
            return View(model);
        }

        try
        {
            await _taskService.UpdateTaskAssignmentAsync(ManagerId, model);

            // Handle file uploads
            Console.WriteLine($"[DEBUG] ManagerController.EditTask: Attachments count: {model.Attachments?.Count ?? 0}");
            if (model.Attachments != null && model.Attachments.Any())
            {
                await _taskService.SaveAttachmentsAsync(model.Id, ManagerId, model.Attachments);
            }
            
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Task updated successfully!" });
            }
            
            TempData["Success"] = "Task updated successfully!";
            return RedirectToAction(nameof(TaskManagement));
        }
        catch (Exception ex)
        {
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
            
            TempData["Error"] = $"Failed to update task: {ex.Message}";
            var vm = await _taskService.GetTaskAssignmentFormAsync(null, ManagerId);
            model.Projects = vm.Projects;
            model.Categories = vm.Categories;
            model.Priorities = vm.Priorities;
            model.Statuses = vm.Statuses;
            model.Employees = vm.Employees;
            model.TaskTypes = vm.TaskTypes;
            return View(model);
        }
    }

    // ============================================
    // TEAM PERFORMANCE
    // ============================================

    public async Task<IActionResult> TeamPerformance(DateTime? startDate, DateTime? endDate)
    {
        var model = await _managerService.GetTeamPerformanceAsync(ManagerId, startDate, endDate);
        return View(model);
    }

    // ============================================
    // PROFILE
    // ============================================

    public async Task<IActionResult> Profile()
    {
        var model = await _userService.GetProfileAsync(ManagerId);
        return View(model);
    }

    // ============================================
    // SETTINGS
    // ============================================

    [HttpGet]
    public IActionResult Settings()
    {
        var model = new SettingsVM
        {
            UserId = ManagerId,
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
            var (success, message) = await _userService.ChangePasswordAsync(ManagerId, model.CurrentPassword, model.NewPassword);
            
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
    // PROJECT MANAGEMENT
    // ============================================

    [HttpGet]
    public async Task<IActionResult> CreateProject()
    {
        var model = await _projectService.GetProjectFormAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProject(ProjectFormVM model)
    {
        if (!ModelState.IsValid)
        {
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Please fill in all required fields." });
            }
            
            var vm = await _projectService.GetProjectFormAsync();
            model.Departments = vm.Departments;
            model.Managers = vm.Managers;
            return View(model);
        }

        try
        {
            // Automatically assign Manager as Project Manager
            model.ManagerId = ManagerId;
            await _projectService.CreateProjectAsync(ManagerId, model);
            
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Project created successfully!" });
            }
            
            TempData["Success"] = "Project created successfully!";
            return RedirectToAction(nameof(MyProjects));
        }
        catch (InvalidOperationException ex)
        {
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
            
            ModelState.AddModelError("Code", ex.Message);
            var vm = await _projectService.GetProjectFormAsync();
            model.Departments = vm.Departments;
            model.Managers = vm.Managers;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditProject(int id)
    {
        var model = await _projectService.GetProjectFormAsync(id);
        if (model.Id == 0) return NotFound();
        
        // Verify manager owns this project
        if (model.ManagerId != ManagerId)
        {
            TempData["Error"] = "You can only edit your own projects.";
            return RedirectToAction(nameof(MyProjects));
        }
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProject(int id, ProjectFormVM model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Please fill in all required fields." });
            }
            
            var vm = await _projectService.GetProjectFormAsync();
            model.Departments = vm.Departments;
            model.Managers = vm.Managers;
            return View(model);
        }

        try
        {
            await _projectService.UpdateProjectAsync(model);
            
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Project updated successfully!" });
            }
            
            TempData["Success"] = "Project updated successfully!";
            return RedirectToAction(nameof(MyProjects));
        }
        catch (InvalidOperationException ex)
        {
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
            
            ModelState.AddModelError("Code", ex.Message);
            var vm = await _projectService.GetProjectFormAsync();
            model.Departments = vm.Departments;
            model.Managers = vm.Managers;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProject(int id)
    {
        try
        {
            // Verify manager owns this project
            var project = await _projectService.GetProjectFormAsync(id);
            if (project.ManagerId != ManagerId)
            {
                TempData["Error"] = "You can only delete your own projects.";
                return RedirectToAction(nameof(MyProjects));
            }
            
            await _projectService.DeleteProjectAsync(id);
            TempData["Success"] = "Project deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to delete project: {ex.Message}";
        }
        return RedirectToAction(nameof(MyProjects));
    }

    [HttpGet]
    public async Task<IActionResult> GenerateProjectCode()
    {
        var projectCode = await _projectService.GenerateNextProjectCodeAsync();
        return Json(new { projectCode });
    }

    // ============================================
    // PROFILE PICTURE MANAGEMENT
    // ============================================

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var model = await _userService.GetProfileForEditAsync(ManagerId);
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

        model.Id = ManagerId; // Ensure user can only update their own profile
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
            ManagerId, 
            profileImage
        );

        return Json(new { success, message, imagePath });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveProfilePicture()
    {
        var (success, message) = await _userService.RemoveProfilePictureAsync(
            ManagerId
        );

        return Json(new { success, message });
    }

    // ============================================
    // NOTIFICATION ENDPOINTS
    // ============================================

    public async Task<IActionResult> Notifications()
    {
        var notifications = await _notificationService.GetUserNotificationsAsync(ManagerId, 50);
        return View(notifications);
    }

    [HttpGet]
    public async Task<IActionResult> GetRecentNotifications()
    {
        var notifications = await _notificationService.GetUserNotificationsAsync(ManagerId, 5);
        return Json(notifications);
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadNotificationCount()
    {
        var count = await _notificationService.GetUnreadCountAsync(ManagerId);
        return Json(new { count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        await _notificationService.MarkAsReadAsync(id, ManagerId);
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        await _notificationService.MarkAllAsReadAsync(ManagerId);
        TempData["Success"] = "All notifications marked as read";
        return RedirectToAction(nameof(Notifications));
    }

    // ============================================
    // TASK DETAILS
    // ============================================

    [HttpGet]
    public async Task<IActionResult> TaskDetails(int id)
    {
        var model = await _taskService.GetTaskDetailsEnhancedAsync(id);
        if (model == null)
        {
            TempData["Error"] = $"Task with ID {id} not found. It may have been deleted.";
            return RedirectToAction(nameof(TaskManagement));
        }
        
        // Security check: Allow if task belongs to manager's project OR created by manager
        bool hasAccess = false;
        
        // Check if task is in a project managed by this manager
        if (model.ProjectId.HasValue)
        {
            var project = await _projectService.GetProjectDetailsAsync(model.ProjectId.Value);
            if (project != null && project.ManagerId == ManagerId)
            {
                hasAccess = true;
            }
        }
        
        // Also allow if manager created or was assigned this task
        if (model.CreatedById == ManagerId || model.AssignedTo == ManagerId)
        {
            hasAccess = true;
        }
        
        if (!hasAccess)
        {
            TempData["Error"] = "You don't have permission to view this task.";
            return RedirectToAction(nameof(TaskManagement));
        }
        
        return View(model);
    }
}

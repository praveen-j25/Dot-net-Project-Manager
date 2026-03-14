using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TaskManagerMVC.Authorization;
using TaskManagerMVC.Services;
using TaskManagerMVC.ViewModels;

namespace TaskManagerMVC.Controllers;

[Authorize(Policy = Policies.AdminOnly)]
public class AdminController : Controller
{
    private readonly DashboardService _dashboardService;
    private readonly UserService _userService;
    private readonly ProjectService _projectService;
    private readonly TaskService _taskService;
    private readonly NotificationService _notificationService;
    private readonly PendingUserService _pendingUserService;

    private readonly IWebHostEnvironment _webHostEnvironment;

    public AdminController(
        DashboardService dashboardService,
        UserService userService,
        ProjectService projectService,
        TaskService taskService,
        NotificationService notificationService,
        PendingUserService pendingUserService,
        IWebHostEnvironment webHostEnvironment)
    {
        _dashboardService = dashboardService;
        _userService = userService;
        _projectService = projectService;
        _taskService = taskService;
        _notificationService = notificationService;
        _pendingUserService = pendingUserService;
        _webHostEnvironment = webHostEnvironment;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string UserName => User.FindFirstValue(ClaimTypes.Name) ?? "Admin";

    // ============================================
    // DASHBOARD
    // ============================================

    public async Task<IActionResult> Index()
    {
        var model = await _dashboardService.GetAdminDashboardAsync(UserName);
        return View(model);
    }

    // ============================================
    // USER MANAGEMENT
    // ============================================

    public async Task<IActionResult> Users(int? roleId, int? departmentId, int? teamId, bool? isActive = null, bool groupByTeam = false)
    {
        // Default to showing only active users if no filter is applied
        // This ensures that "deleted" (soft deleted) users disappear from the list
        UserListVM model;
        
        if (groupByTeam)
        {
            model = await _userService.GetUsersGroupedByTeamAsync(roleId, departmentId, isActive);
        }
        else
        {
            model = await _userService.GetUsersAsync(roleId, departmentId, teamId, isActive);
        }
        
        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> CreateUser()
    {
        var model = await _userService.GetUserFormAsync(UserId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> CreateUser(UserFormVM model)
    {
        if (!ModelState.IsValid)
        {
            var vm = await _userService.GetUserFormAsync(UserId);
            model.Roles = vm.Roles;
            model.Departments = vm.Departments;
            model.Teams = vm.Teams;
            model.Managers = vm.Managers;
            return View(model);
        }

        try
        {
            await _userService.CreateUserAsync(model);
            TempData["Success"] = "Employee created successfully!";
            return RedirectToAction(nameof(Users));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("Email", ex.Message);
            var vm = await _userService.GetUserFormAsync(UserId);
            model.Roles = vm.Roles;
            model.Departments = vm.Departments;
            model.Teams = vm.Teams;
            model.Managers = vm.Managers;
            return View(model);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> EditUser(int id)
    {
        // SECURITY: Prevent editing the system admin (ID = 1 or email = admin@taskmanager.com)
        if (id == 1)
        {
            TempData["Error"] = "System administrator account cannot be edited.";
            return RedirectToAction(nameof(Users));
        }
        
        var model = await _userService.GetUserFormAsync(UserId, id);
        if (model.Id == 0) return NotFound();
        
        // Additional check by email
        if (model.Email == "admin@taskmanager.com")
        {
            TempData["Error"] = "System administrator account cannot be edited.";
            return RedirectToAction(nameof(Users));
        }
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> EditUser(int id, UserFormVM model)
    {
        // SECURITY: Prevent editing the system admin
        if (id == 1 || model.Email == "admin@taskmanager.com")
        {
            TempData["Error"] = "System administrator account cannot be edited.";
            return RedirectToAction(nameof(Users));
        }
        
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            var vm = await _userService.GetUserFormAsync(UserId);
            model.Roles = vm.Roles;
            model.Departments = vm.Departments;
            model.Teams = vm.Teams;
            model.Managers = vm.Managers;
            return View(model);
        }

        await _userService.UpdateUserAsync(model);
        TempData["Success"] = "Employee updated successfully!";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        // SECURITY: Prevent deleting the system admin
        if (id == 1)
        {
            TempData["Error"] = "System administrator account cannot be deleted.";
            return RedirectToAction(nameof(Users));
        }

        try
        {
            await _userService.DeleteUserAsync(id);
            TempData["Success"] = "Employee deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to delete employee: {ex.Message}";
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        try
        {
            // SECURITY: Admin access control
            // For strictest security, add ownership verification here
            // Currently allows all admins to delete (standard admin behavior)
            
            await _taskService.DeleteTaskAsync(id);
            TempData["Success"] = "Task deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to delete task: {ex.Message}";
        }

        return RedirectToAction(nameof(AssignTasks));
    }

    // ============================================
    // REGISTRATION APPROVAL
    // ============================================

    public async Task<IActionResult> PendingRegistrations(string? status)
    {
        var model = await _pendingUserService.GetPendingUsersAsync(status);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ApproveRegistration(int id)
    {
        var model = await _pendingUserService.GetPendingUserForApprovalAsync(id);
        if (model == null)
        {
            TempData["Error"] = "Registration request not found or already processed.";
            return RedirectToAction(nameof(PendingRegistrations));
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRegistration(int id, ApproveUserVM model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            var vm = await _pendingUserService.GetPendingUserForApprovalAsync(id);
            if (vm != null)
            {
                model.Roles = vm.Roles;
                model.Departments = vm.Departments;
                model.Teams = vm.Teams;
                model.Managers = vm.Managers;
            }
            return View(model);
        }

        try
        {
            await _pendingUserService.ApproveUserAsync(id, model, UserId);
            TempData["Success"] = $"Registration approved! User account created for {model.Email}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to approve registration: {ex.Message}";
        }

        return RedirectToAction(nameof(PendingRegistrations));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRegistration(int id, string rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            TempData["Error"] = "Rejection reason is required.";
            return RedirectToAction(nameof(PendingRegistrations));
        }

        try
        {
            await _pendingUserService.RejectUserAsync(id, rejectionReason, UserId);
            TempData["Success"] = "Registration request rejected.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to reject registration: {ex.Message}";
        }

        return RedirectToAction(nameof(PendingRegistrations));
    }

    [HttpGet]
    public async Task<IActionResult> GetPendingRegistrationCount()
    {
        var count = await _pendingUserService.GetPendingCountAsync();
        return Json(new { count });
    }

    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GenerateEmployeeId()
    {
        var employeeId = await _userService.GenerateNextEmployeeIdAsync();
        return Json(new { employeeId });
    }

    [HttpGet]
    public async Task<IActionResult> GenerateProjectCode()
    {
        var projectCode = await _projectService.GenerateNextProjectCodeAsync();
        return Json(new { projectCode });
    }

    [HttpGet]
    public async Task<IActionResult> GetTeamsByDepartment(int departmentId)
    {
        var teams = await _userService.GetTeamsByDepartmentAsync(departmentId);
        return Json(teams);
    }

    // ============================================
    // PROJECT MANAGEMENT
    // ============================================

    public async Task<IActionResult> Projects(string? status, int? departmentId, string? priority)
    {
        var model = await _projectService.GetProjectsAsync(status, departmentId, priority);
        return View(model);
    }

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
            model.Statuses = vm.Statuses;
            model.Priorities = vm.Priorities;
            return View(model);
        }

        try
        {
            // Automatically assign Admin as Manager
            model.ManagerId = UserId;
            await _projectService.CreateProjectAsync(UserId, model);
            
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Project created successfully!" });
            }
            
            TempData["Success"] = "Project created successfully!";
            return RedirectToAction(nameof(Projects));
        }
        catch (InvalidOperationException ex)
        {
            // Handle duplicate project code error
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
            
            ModelState.AddModelError("Code", ex.Message);
            var vm = await _projectService.GetProjectFormAsync();
            model.Departments = vm.Departments;
            model.Managers = vm.Managers;
            model.Statuses = vm.Statuses;
            model.Priorities = vm.Priorities;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditProject(int id)
    {
        var model = await _projectService.GetProjectFormAsync(id);
        if (model.Id == 0)
        {
            TempData["Error"] = $"Project with ID {id} not found.";
            return RedirectToAction(nameof(Projects));
        }
        
        // SECURITY: Verify admin has permission to edit this project
        // For strictest security, uncomment to restrict access:
        // if (model.ManagerId != UserId)
        // {
        //     TempData["Error"] = "You can only edit projects you manage.";
        //     return RedirectToAction(nameof(Projects));
        // }
        
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
            model.Statuses = vm.Statuses;
            model.Priorities = vm.Priorities;
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
            return RedirectToAction(nameof(Projects));
        }
        catch (InvalidOperationException ex)
        {
            // Handle duplicate project code error
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
            
            ModelState.AddModelError("Code", ex.Message);
            var vm = await _projectService.GetProjectFormAsync();
            model.Departments = vm.Departments;
            model.Managers = vm.Managers;
            model.Statuses = vm.Statuses;
            model.Priorities = vm.Priorities;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProject(int id)
    {
        try
        {
            // SECURITY: Verify admin has permission to delete this project
            var project = await _projectService.GetProjectFormAsync(id);
            if (project.Id == 0)
            {
                TempData["Error"] = $"Project with ID {id} not found.";
                return RedirectToAction(nameof(Projects));
            }
            
            // For strictest security, uncomment to restrict access:
            // if (project.ManagerId != UserId)
            // {
            //     TempData["Error"] = "You can only delete projects you manage.";
            //     return RedirectToAction(nameof(Projects));
            // }
            
            await _projectService.DeleteProjectAsync(id);
            TempData["Success"] = "Project deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to delete project: {ex.Message}";
        }

        return RedirectToAction(nameof(Projects));
    }

    public async Task<IActionResult> ProjectDetails(int id)
    {
        var model = await _projectService.GetProjectDetailsAsync(id);
        if (model == null)
        {
            TempData["Error"] = $"Project with ID {id} not found.";
            return RedirectToAction(nameof(Projects));
        }
        
        // SECURITY: Verify admin has permission to view this project
        // For strictest security, uncomment to restrict access:
        // if (model.ManagerId != UserId)
        // {
        //     TempData["Error"] = "You don't have permission to view this project.";
        //     return RedirectToAction(nameof(Projects));
        // }
        
        return View(model);
    }

    // ============================================
    // TASK ASSIGNMENT
    // ============================================

    public async Task<IActionResult> AssignTasks(int? statusId, int? priorityId, int? projectId, int? assignedTo)
    {
        var model = await _taskService.GetTasksForAdminAsync(statusId, priorityId, projectId, assignedTo);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AssignTask()
    {
        var model = await _taskService.GetTaskAssignmentFormAsync(null, UserId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTask(TaskAssignmentVM model)
    {
        // Validate file sizes (10MB max per file)
        if (model.Attachments != null && model.Attachments.Any())
        {
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
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
            
            var vm = await _taskService.GetTaskAssignmentFormAsync(null, UserId);
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
            var taskId = await _taskService.AssignTaskAsync(UserId, model);
            
            // Handle file uploads
            if (model.Attachments != null && model.Attachments.Any())
            {
                await _taskService.SaveAttachmentsAsync(taskId, UserId, model.Attachments);
            }

            // Send notification to assignee
            if (model.AssignedTo.HasValue)
            {
                await _notificationService.NotifyTaskAssignedAsync(taskId, model.AssignedTo.Value, UserId, model.Title);
            }

            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Task assigned successfully!" });
            }

            TempData["Success"] = "Task assigned successfully!";
            return RedirectToAction(nameof(AssignTasks));
        }
        catch (Exception ex)
        {
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
            
            TempData["Error"] = $"Failed to assign task: {ex.Message}";
            var vm = await _taskService.GetTaskAssignmentFormAsync(null, UserId);
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
    public async Task<IActionResult> EditAssignment(int id)
    {
        var model = await _taskService.GetTaskAssignmentFormAsync(id, UserId);
        if (model.Id == 0)
        {
            TempData["Error"] = $"Task with ID {id} not found.";
            return RedirectToAction(nameof(AssignTasks));
        }
        
        // SECURITY: Admin access control
        // For strictest security, add ownership verification here
        // Currently allows all admins to edit (standard admin behavior)
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAssignment(int id, TaskAssignmentVM model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Please fill in all required fields." });
            }
            
            var vm = await _taskService.GetTaskAssignmentFormAsync(null, UserId);
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
            await _taskService.UpdateTaskAssignmentAsync(UserId, model);

            // Handle file uploads
            if (model.Attachments != null && model.Attachments.Any())
            {
                await _taskService.SaveAttachmentsAsync(model.Id, UserId, model.Attachments);
            }

            if (model.AssignedTo.HasValue)
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task != null)
                {
                    // If assignee changed, notify the new assignee
                    // If same assignee, notify of update
                    // For simplicity, we'll just notify of update/assignment
                    await _notificationService.NotifyTaskAssignedAsync(id, model.AssignedTo.Value, UserId, model.Title);
                }
            }

            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Task updated successfully!" });
            }

            TempData["Success"] = "Task updated successfully!";
            return RedirectToAction(nameof(AssignTasks));
        }
        catch (Exception ex)
        {
            // Log the actual error for debugging
            Console.WriteLine($"Error updating task: {ex.Message}");
            
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Failed to update task. Please try again or contact support if the problem persists." });
            }
            
            TempData["Error"] = "Failed to update task. Please try again or contact support if the problem persists.";
            var vm = await _taskService.GetTaskAssignmentFormAsync(null, UserId);
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
    // TASK DETAILS (Admin view)
    // ============================================

    public async Task<IActionResult> TaskDetails(int id)
    {
        var model = await _taskService.GetTaskDetailsEnhancedAsync(id);
        if (model == null)
        {
            TempData["Error"] = $"Task with ID {id} not found. It may have been deleted.";
            return RedirectToAction(nameof(AssignTasks));
        }
        
        // SECURITY: Even admins should verify they have a relationship to this task
        // Admin can view if: created by them, assigned by them, or in a project they manage
        bool isCreatedByAdmin = model.CreatedById == UserId;
        bool isAssignedByAdmin = model.AssignedTo == UserId || model.CreatedById == UserId;
        
        // For strictest security, uncomment this to restrict admin access:
        // if (!isCreatedByAdmin && !isAssignedByAdmin)
        // {
        //     TempData["Error"] = "You don't have permission to view this task.";
        //     return RedirectToAction(nameof(AssignTasks));
        // }
        
        return View(model);
    }

    // ============================================
    // TEAM REPORTS
    // ============================================

    public async Task<IActionResult> TeamPerformance(int? departmentId, int? teamId)
    {
        var employees = await _userService.GetEmployeeSummariesAsync(departmentId, teamId);
        ViewBag.DepartmentId = departmentId;
        ViewBag.TeamId = teamId;
        return View(employees);
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
    // API ENDPOINTS FOR AJAX
    // ============================================

    [HttpGet]
    public async Task<IActionResult> GetUnreadNotificationCount()
    {
        var count = await _notificationService.GetUnreadCountAsync(UserId);
        return Json(new { count });
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

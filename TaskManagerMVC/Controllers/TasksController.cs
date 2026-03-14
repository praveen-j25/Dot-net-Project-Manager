using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagerMVC.Services;
using TaskManagerMVC.ViewModels;

namespace TaskManagerMVC.Controllers;

[Authorize]
public class TasksController : Controller
{
    private readonly TaskService _taskService;
    private readonly IWebHostEnvironment _env;

    public TasksController(TaskService taskService, IWebHostEnvironment env)
    {
        _taskService = taskService;
        _env = env;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index(int? statusId, int? priorityId, int? categoryId)
    {
        var model = await _taskService.GetTasksAsync(UserId, statusId, priorityId, categoryId);
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null) return NotFound();

        // Security: Allow Creator, Assignee, Project Manager, or Admin
        bool isCreator = task.CreatedById == UserId;
        bool isAssignee = task.AssignedToId == UserId;
        bool isProjectManager = task.ProjectManagerId == UserId;
        bool isAdmin = User.IsInRole("Admin");

        if (!isCreator && !isAssignee && !isProjectManager && !isAdmin)
        {
            return Forbid();
        }

        // Load attachments
        task.FileAttachments = await _taskService.GetAttachmentsAsync(id);
        return View(task);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await _taskService.GetTaskFormAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskFormVM model)
    {
        // Validate file sizes (10MB max per file)
        if (model.Attachments != null && model.Attachments.Any())
        {
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            var oversizedFiles = model.Attachments.Where(f => f.Length > maxFileSize).ToList();
            
            if (oversizedFiles.Any())
            {
                var fileNames = string.Join(", ", oversizedFiles.Select(f => f.FileName));
                ModelState.AddModelError("Attachments", $"The following files exceed the 10MB limit: {fileNames}");
            }
        }
        
        if (!ModelState.IsValid)
        {
            var vm = await _taskService.GetTaskFormAsync();
            model.Categories = vm.Categories;
            model.Priorities = vm.Priorities;
            model.Statuses = vm.Statuses;
            return View(model);
        }

        var taskId = await _taskService.CreateTaskAsync(model, UserId);

        // Handle file uploads
        if (model.Attachments?.Any() == true)
        {
            await _taskService.SaveAttachmentsAsync(taskId, UserId, model.Attachments);
        }

        TempData["Success"] = "Task created successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _taskService.GetTaskFormAsync(id);
        if (model.Id == 0) return NotFound();
        
        // SECURITY: Verify user created this task
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null) return NotFound();
        
        if (task.CreatedById != UserId)
        {
            TempData["Error"] = "You can only edit tasks you created.";
            return RedirectToAction(nameof(Index));
        }
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TaskFormVM model)
    {
        if (id != model.Id) return BadRequest();

        // SECURITY: Verify user created this task
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null) return NotFound();
        
        if (task.CreatedById != UserId)
        {
            TempData["Error"] = "You can only edit tasks you created.";
            return RedirectToAction(nameof(Index));
        }

        // Validate file sizes (10MB max per file)
        if (model.Attachments != null && model.Attachments.Any())
        {
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            var oversizedFiles = model.Attachments.Where(f => f.Length > maxFileSize).ToList();
            
            if (oversizedFiles.Any())
            {
                var fileNames = string.Join(", ", oversizedFiles.Select(f => f.FileName));
                ModelState.AddModelError("Attachments", $"The following files exceed the 10MB limit: {fileNames}");
            }
        }

        if (!ModelState.IsValid)
        {
            var vm = await _taskService.GetTaskFormAsync();
            model.Categories = vm.Categories;
            model.Priorities = vm.Priorities;
            model.Statuses = vm.Statuses;
            return View(model);
        }

        await _taskService.UpdateTaskAsync(model, UserId);

        // Handle file uploads
        if (model.Attachments?.Any() == true)
        {
            await _taskService.SaveAttachmentsAsync(id, UserId, model.Attachments);
        }

        TempData["Success"] = "Task updated successfully!";
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        // SECURITY: Verify user created this task
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null) return NotFound();
        
        if (task.CreatedById != UserId)
        {
            TempData["Error"] = "You can only delete tasks you created.";
            return RedirectToAction(nameof(Index));
        }
        
        await _taskService.DeleteTaskAsync(id);
        TempData["Success"] = "Task deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttachment(int id, int taskId)
    {
        // SECURITY: Verify user created the task that owns this attachment
        var task = await _taskService.GetTaskByIdAsync(taskId);
        if (task == null) return NotFound();
        
        if (task.CreatedById != UserId)
        {
            TempData["Error"] = "You can only delete attachments from tasks you created.";
            return RedirectToAction(nameof(Index));
        }
        
        await _taskService.DeleteAttachmentAsync(id);
        TempData["Success"] = "Attachment deleted.";
        return RedirectToAction(nameof(Details), new { id = taskId });
    }
}

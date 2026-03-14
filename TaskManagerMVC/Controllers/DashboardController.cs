using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagerMVC.Services;

namespace TaskManagerMVC.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly TaskService _taskService;

    public DashboardController(TaskService taskService)
    {
        _taskService = taskService;
    }

    public async Task<IActionResult> Index()
    {
        // Redirect to role-based dashboard
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "Employee";
        
        // Redirect based on role
        if (role == "Administrator" || role == "Admin")
        {
            return RedirectToAction("Index", "Admin");
        }
        
        if (role == "Manager")
        {
            return RedirectToAction("Index", "Manager");
        }

        // Employee dashboard
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.FindFirstValue("FirstName") ?? "User";
        
        var dashboard = await _taskService.GetDashboardAsync(userId, userName);
        return View(dashboard);
    }
}

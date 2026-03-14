using Microsoft.AspNetCore.Authorization;

namespace TaskManagerMVC.Authorization;

// Custom authorization requirement for role-based access
public class RoleRequirement : IAuthorizationRequirement
{
    public string[] AllowedRoles { get; }

    public RoleRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }
}

// Handler for role requirement
public class RoleRequirementHandler : AuthorizationHandler<RoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Task.CompletedTask;
        }

        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (userRole != null && requirement.AllowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

// Permission-based requirement
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

// Handler for permission requirement
public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Task.CompletedTask;
        }

        var roleId = context.User.FindFirst("RoleId")?.Value;

        // Admin (RoleId = 1) has all permissions
        if (roleId == "1")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check specific permissions based on requirement
        var hasPermission = requirement.Permission switch
        {
            "users.manage" => roleId == "1", // Only Admin
            "users.view" => roleId is "1" or "2", // Admin and Manager
            "projects.manage" => roleId is "1" or "2", // Admin and Manager
            "projects.view" => true, // All authenticated users
            "tasks.assign" => roleId is "1" or "2", // Admin and Manager
            "tasks.manage_all" => roleId is "1" or "2", // Admin and Manager
            "tasks.manage_own" => true, // All authenticated users
            "reports.view" => roleId is "1" or "2", // Admin and Manager
            "reports.export" => roleId == "1", // Only Admin
            "settings.manage" => roleId == "1", // Only Admin
            _ => false
        };

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

// Resource-based authorization (for task ownership)
public class TaskOwnerRequirement : IAuthorizationRequirement { }

public class TaskOwnerRequirementHandler : AuthorizationHandler<TaskOwnerRequirement, int>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TaskOwnerRequirementHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TaskOwnerRequirement requirement,
        int taskId)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Task.CompletedTask;
        }

        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var roleId = context.User.FindFirst("RoleId")?.Value;

        // Admin and Manager can access all tasks
        if (roleId is "1" or "2")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // For employees, check if they own or are assigned to the task
        // This would require database lookup in real implementation
        // For now, we'll allow access and let the service layer handle it
        context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

// Static class for policy names
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string ManagerOrAbove = "ManagerOrAbove";
    public const string ManagerOnly = "ManagerOnly";
    public const string EmployeeOnly = "EmployeeOnly";
    public const string AllAuthenticated = "AllAuthenticated";
    
    // Permission-based policies
    public const string ManageUsers = "ManageUsers";
    public const string ViewUsers = "ViewUsers";
    public const string ManageProjects = "ManageProjects";
    public const string ViewProjects = "ViewProjects";
    public const string AssignTasks = "AssignTasks";
    public const string ManageAllTasks = "ManageAllTasks";
    public const string ManageOwnTasks = "ManageOwnTasks";
    public const string ViewReports = "ViewReports";
    public const string ExportReports = "ExportReports";
    public const string ManageSettings = "ManageSettings";
}

// Static class for roles
public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";
}

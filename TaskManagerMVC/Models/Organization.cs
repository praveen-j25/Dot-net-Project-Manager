namespace TaskManagerMVC.Models;

/// <summary>
/// User role for access control (Admin, Manager, Employee)
/// </summary>
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Permissions { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public ICollection<User>? Users { get; set; }

    // Constants for role IDs
    public const int AdminRoleId = 1;
    public const int ManagerRoleId = 2;
    public const int EmployeeRoleId = 3;
}

/// <summary>
/// Department/division in the organization
/// </summary>
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int? ManagerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public User? Manager { get; set; }
    public ICollection<User>? Users { get; set; }
    public ICollection<Team>? Teams { get; set; }
    public ICollection<Project>? Projects { get; set; }
}

/// <summary>
/// Team within a department
/// </summary>
public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int? DepartmentId { get; set; }
    public int? TeamLeadId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public Department? Department { get; set; }
    public User? TeamLead { get; set; }
    public ICollection<User>? Members { get; set; }
}

/// <summary>
/// Project that contains multiple tasks
/// </summary>
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Code { get; set; }
    public int? DepartmentId { get; set; }
    public int? ManagerId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Budget { get; set; }
    public string Status { get; set; } = "planning";
    public string Priority { get; set; } = "medium";
    public bool IsActive { get; set; } = true;
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    public Department? Department { get; set; }
    public User? Manager { get; set; }
    public User? Creator { get; set; }
    public ICollection<TaskItem>? Tasks { get; set; }
    public ICollection<ProjectMember>? Members { get; set; }

    // Computed
    public int DaysRemaining => EndDate.HasValue ? (EndDate.Value.Date - DateTime.Today).Days : 0;
}

/// <summary>
/// Project member assignment
/// </summary>
public class ProjectMember
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "member";
    public DateTime JoinedAt { get; set; } = DateTime.Now;

    // Navigation
    public Project? Project { get; set; }
    public User? User { get; set; }
}

/// <summary>
/// Task comment/response from employees
/// </summary>
public class TaskComment
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string Comment { get; set; } = "";
    public string CommentType { get; set; } = "comment"; // comment, status_update, progress_update, time_log, system
    public bool IsInternal { get; set; } = false;
    public int? ParentCommentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    public TaskItem? Task { get; set; }
    public User? User { get; set; }
    public TaskComment? ParentComment { get; set; }
    public ICollection<TaskComment>? Replies { get; set; }
}

/// <summary>
/// Audit trail for task changes
/// </summary>
public class TaskActivityLog
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? FieldChanged { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public TaskItem? Task { get; set; }
    public User? User { get; set; }
}

/// <summary>
/// Task file attachment
/// </summary>
public class TaskAttachment
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UploadedBy { get; set; }
    public string FileName { get; set; } = "";
    public string OriginalName { get; set; } = "";
    public string? FileType { get; set; }
    public int FileSize { get; set; }
    public string FilePath { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public TaskItem? Task { get; set; }
    public User? Uploader { get; set; }

    // Helper
    public string FileSizeDisplay => FileSize < 1024 ? $"{FileSize} B" 
        : FileSize < 1048576 ? $"{FileSize / 1024} KB" 
        : $"{FileSize / 1048576} MB";
}

/// <summary>
/// Time logging for tasks
/// </summary>
public class TimeLog
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public int? ProjectId { get; set; }
    public decimal HoursLogged { get; set; }
    public DateTime LogDate { get; set; }
    public string? Description { get; set; }
    public bool IsBillable { get; set; } = false;
    public bool IsApproved { get; set; } = false;
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public TaskItem? Task { get; set; }
    public User? User { get; set; }
    public Project? Project { get; set; }
    public User? Approver { get; set; }
}

/// <summary>
/// User notification
/// </summary>
public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "system"; // task_assigned, task_updated, comment_added, deadline_reminder, system, mention
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public User? User { get; set; }
}

/// <summary>
/// Multiple task assignees
/// </summary>
public class TaskAssignee
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public int AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.Now;

    // Navigation
    public TaskItem? Task { get; set; }
    public User? User { get; set; }
    public User? Assigner { get; set; }
}

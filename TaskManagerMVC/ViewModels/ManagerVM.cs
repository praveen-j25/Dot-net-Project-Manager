using System.ComponentModel.DataAnnotations;

namespace TaskManagerMVC.ViewModels;

/// <summary>
/// Manager Dashboard ViewModel
/// </summary>
public class ManagerDashboardVM
{
    // Team Stats
    public int TotalTeamMembers { get; set; }
    public int ActiveToday { get; set; }
    public int OnLeave { get; set; }

    // Project Stats
    public int ActiveProjects { get; set; }
    public int ProjectsOnTrack { get; set; }
    public int ProjectsAtRisk { get; set; }
    public int ProjectsDelayed { get; set; }

    // Task Stats
    public int TotalTasks { get; set; }
    public int CompletedThisWeek { get; set; }
    public int OverdueTasks { get; set; }
    public int InProgressTasks { get; set; }

    // Performance
    public decimal TeamCompletionRate { get; set; }
    public decimal AverageTaskTime { get; set; }

    // Lists
    public List<ActivityVM> RecentActivities { get; set; } = new();
    public List<TopPerformerVM> TopPerformers { get; set; } = new();
    public List<UpcomingDeadlineVM> UpcomingDeadlines { get; set; } = new();
}

/// <summary>
/// Team Members List ViewModel
/// </summary>
public class TeamMembersVM
{
    public List<TeamMemberVM> Members { get; set; } = new();
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
}

/// <summary>
/// Individual Team Member ViewModel
/// </summary>
public class TeamMemberVM
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public string RoleName { get; set; } = "";
    public string? DepartmentName { get; set; }
    public string? TeamName { get; set; }
    public bool IsActive { get; set; }
    public int ActiveTasks { get; set; }
    public int CompletedTasks { get; set; }
    public decimal CompletionRate => ActiveTasks + CompletedTasks > 0 
        ? (decimal)CompletedTasks / (ActiveTasks + CompletedTasks) * 100 
        : 0;
}

/// <summary>
/// Manager Projects List ViewModel
/// </summary>
public class ManagerProjectsVM
{
    public List<ManagerProjectVM> Projects { get; set; } = new();
    public string? StatusFilter { get; set; }
}

/// <summary>
/// Individual Manager Project ViewModel
/// </summary>
public class ManagerProjectVM
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Budget { get; set; }
    public string? DepartmentName { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public decimal Progress => TotalTasks > 0 
        ? (decimal)CompletedTasks / TotalTasks * 100 
        : 0;
    public int DaysRemaining => EndDate.HasValue 
        ? (EndDate.Value.Date - DateTime.Today).Days 
        : 0;
}

/// <summary>
/// Activity Log Item
/// </summary>
public class ActivityVM
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string TimeAgo
    {
        get
        {
            var span = DateTime.Now - CreatedAt;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return CreatedAt.ToString("MMM dd");
        }
    }
}

/// <summary>
/// Top Performer ViewModel
/// </summary>
public class TopPerformerVM
{
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public int CompletedTasks { get; set; }
    public decimal CompletionRate { get; set; }
    public int OnTimeTasks { get; set; }
}

/// <summary>
/// Upcoming Deadline ViewModel
/// </summary>
public class UpcomingDeadlineVM
{
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public string AssigneeName { get; set; } = "";
    public DateTime DueDate { get; set; }
    public int DaysUntilDue => (DueDate.Date - DateTime.Today).Days;
    public string Priority { get; set; } = "";
}

/// <summary>
/// Team Performance ViewModel
/// </summary>
public class TeamPerformanceVM : IValidatableObject
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<TeamMemberPerformanceVM> MemberPerformance { get; set; } = new();
    public decimal OverallCompletionRate { get; set; }
    public decimal AverageTaskDuration { get; set; }
    public int TotalTasksCompleted { get; set; }
    public int TotalTasksOverdue { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue)
        {
            if (EndDate.Value.Date < StartDate.Value.Date)
            {
                yield return new ValidationResult(
                    "End date must be on or after the start date.",
                    new[] { nameof(EndDate) });
            }
        }
    }
}

/// <summary>
/// Individual Team Member Performance
/// </summary>
public class TeamMemberPerformanceVM
{
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public int TasksAssigned { get; set; }
    public int TasksCompleted { get; set; }
    public int TasksInProgress { get; set; }
    public int TasksOverdue { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AverageCompletionTime { get; set; }
}

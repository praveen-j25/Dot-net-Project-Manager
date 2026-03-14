using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskManagerMVC.Attributes;

namespace TaskManagerMVC.ViewModels;

// =====================================================
// ADMIN DASHBOARD VIEW MODELS
// =====================================================

public class AdminDashboardVM
{
    public string UserName { get; set; } = "";
    public int TotalEmployees { get; set; }
    public int ActiveProjects { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int InReviewTasks { get; set; }
    public int OverdueTasks { get; set; }
    public decimal TotalHoursLogged { get; set; }

    // Charts data
    public Dictionary<string, int> TasksByStatus { get; set; } = new();
    public Dictionary<string, int> TasksByPriority { get; set; } = new();
    public Dictionary<string, int> TasksByDepartment { get; set; } = new();
    public List<MonthlyTaskData> MonthlyTrends { get; set; } = new();

    // Lists
    public List<EmployeeSummaryVM> TopPerformers { get; set; } = new();
    public List<ProjectSummaryVM> ActiveProjectsList { get; set; } = new();
    public List<TaskItemVM> RecentTasks { get; set; } = new();
    public List<TaskItemVM> OverdueTasksList { get; set; } = new();
    public List<NotificationVM> RecentNotifications { get; set; } = new();
}

public class MonthlyTaskData
{
    public string Month { get; set; } = "";
    public int Created { get; set; }
    public int Completed { get; set; }
}

public class EmployeeSummaryVM
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string? ProfileImage { get; set; }
    public string? JobTitle { get; set; }
    public string? DepartmentName { get; set; }
    public string? Department => DepartmentName;
    public string? TeamName { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int OverdueTasks { get; set; }
    public decimal HoursLogged { get; set; }
    public int CompletionRate => TotalTasks > 0 ? (CompletedTasks * 100 / TotalTasks) : 0;
    public string Initials => string.Join("", FullName.Split(' ').Where(s => !string.IsNullOrEmpty(s)).Select(s => s[0].ToString().ToUpper()).Take(2));
}

public class ProjectSummaryVM
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public string? ManagerName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int TeamSize { get; set; }
    public int Progress => TotalTasks > 0 ? (CompletedTasks * 100 / TotalTasks) : 0;
    public int ProgressPercent => Progress;
}

// =====================================================
// EMPLOYEE DASHBOARD VIEW MODELS
// =====================================================

public class EmployeeDashboardVM
{
    public string UserName { get; set; } = "";
    public string EmployeeName => UserName;
    public string? JobTitle { get; set; }
    public string? DepartmentName { get; set; }
    public string? TeamName { get; set; }

    // Stats
    public int MyTotalTasks { get; set; }
    public int TotalTasks => MyTotalTasks;
    public int MyCompletedTasks { get; set; }
    public int CompletedTasks => MyCompletedTasks;
    public int MyInProgressTasks { get; set; }
    public int InProgressTasks => MyInProgressTasks;
    public int MyOverdueTasks { get; set; }
    public decimal MyHoursThisWeek { get; set; }
    public decimal MyHoursThisMonth { get; set; }
    public int CompletionRate => MyTotalTasks > 0 ? (MyCompletedTasks * 100 / MyTotalTasks) : 0;

    // Task lists
    public List<TaskItemVM> TodaysTasks { get; set; } = new();
    public List<TaskItemVM> TodayTasks => TodaysTasks;
    public List<TaskItemVM> UpcomingTasks { get; set; } = new();
    public List<TaskItemVM> OverdueTasks { get; set; } = new();
    public List<TaskItemVM> RecentlyCompleted { get; set; } = new();
    public List<TaskItemVM> RecentTasks { get; set; } = new();

    // Activity
    public List<ActivityLogVM> RecentActivity { get; set; } = new();
    public List<NotificationVM> Notifications { get; set; } = new();
    public int UnreadNotifications => Notifications?.Count(n => !n.IsRead) ?? 0;
}

// =====================================================
// TASK MANAGEMENT VIEW MODELS
// =====================================================

public class TaskAssignmentVM : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200)]
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    [Display(Name = "Project")]
    public int? ProjectId { get; set; }

    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Required]
    [Display(Name = "Priority")]
    public int PriorityId { get; set; } = 2;

    [Display(Name = "Status")]
    public int StatusId { get; set; } = 1;

    [Display(Name = "Task Type")]
    public string TaskType { get; set; } = "task";

    [Display(Name = "Assign To")]
    public int? AssignedTo { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    // Removed [FutureDate] to allow editing existing tasks
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "Due date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Due Date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [FutureDate(AllowToday = true, ErrorMessage = "Due date cannot be in the past")]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);

    [Display(Name = "Estimated Hours")]
    [Range(0, 1000, ErrorMessage = "Estimated hours must be between 0 and 1000")]
    public decimal? EstimatedHours { get; set; }

    [Display(Name = "Billable")]
    public bool IsBillable { get; set; }

    [Display(Name = "Hourly Rate")]
    [Range(0, 10000, ErrorMessage = "Hourly rate must be between 0 and 10000")]
    public decimal HourlyRate { get; set; }

    public string? Tags { get; set; }

    [Display(Name = "Attachments")]
    public List<IFormFile>? Attachments { get; set; }

    // Dropdowns
    public SelectList? Projects { get; set; }
    public SelectList? Categories { get; set; }
    public SelectList? Priorities { get; set; }
    public SelectList? Statuses { get; set; }
    public SelectList? Employees { get; set; }
    public SelectList? TaskTypes { get; set; }

    // Custom validation
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Enforce future StartDate for new tasks only
        if (Id == 0 && StartDate.HasValue)
        {
            if (StartDate.Value.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Start date cannot be in the past for new tasks.",
                    new[] { nameof(StartDate) });
            }
        }

        // Validate due date is after start date
        if (StartDate.HasValue && DueDate != default)
        {
            if (DueDate.Date < StartDate.Value.Date)
            {
                yield return new ValidationResult(
                    "Due date must be on or after the start date",
                    new[] { nameof(DueDate) }
                );
            }
        }

        // Validate due date is not too far in the future (optional - 2 years max)
        if (DueDate != default)
        {
            if (DueDate.Date > DateTime.Today.AddYears(2))
            {
                yield return new ValidationResult(
                    "Due date cannot be more than 2 years in the future",
                    new[] { nameof(DueDate) }
                );
            }
        }
    }
}

public class TaskResponseVM
{
    public int TaskId { get; set; }

    [Display(Name = "New Status")]
    public int? StatusId { get; set; }

    [Display(Name = "Progress (%)")]
    [Range(0, 100)]
    public int? Progress { get; set; }

    [Display(Name = "Hours Worked")]
    [Range(0, 24)]
    public decimal? HoursWorked { get; set; }

    [Required]
    [Display(Name = "Comment")]
    public string Comment { get; set; } = "";

    [Display(Name = "Internal Note")]
    public bool IsInternal { get; set; }

    public SelectList? Statuses { get; set; }
}

public class TaskDetailsEnhancedVM : TaskDetailsVM
{
    // Project info

    public string? ProjectName { get; set; }
    public string? ProjectCode { get; set; }

    // Assignment info
    public int? AssignedTo { get; set; }
    public string? AssigneeName { get; set; }
    public string? AssigneeImage { get; set; }
    public string? AssignedByName { get; set; }
    public DateTime? AssignedAt { get; set; }

    // Creator info
    public new int CreatedById { get; set; }
    public string? CreatorName { get; set; }

    // Extra details
    public string TaskType { get; set; } = "task";
    public DateTime? StartDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }
    public int Progress { get; set; }
    public bool IsBillable { get; set; }
    public string? Tags { get; set; }

    // Related data
    public List<TaskCommentVM> Comments { get; set; } = new();
    public List<ActivityLogVM> ActivityLog { get; set; } = new();
    public List<AttachmentVM> Attachments { get; set; } = new();
    public List<TimeLogVM> TimeLogs { get; set; } = new();
    public List<TaskItemVM> SubTasks { get; set; } = new();

    // Response form
    public TaskResponseVM Response { get; set; } = new();
}

// =====================================================
// COMMENT & ACTIVITY VIEW MODELS
// =====================================================

public class TaskCommentVM
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public string? UserImage { get; set; }
    public string Comment { get; set; } = "";
    public string CommentType { get; set; } = "comment";
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TaskCommentVM> Replies { get; set; } = new();
}

public class ActivityLogVM
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string UserName { get; set; } = "";
    public string? UserImage { get; set; }
    public string Action { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? FieldChanged { get; set; }
    public DateTime CreatedAt { get; set; }

    public string ActionDescription => FieldChanged switch
    {
        "status" => $"changed status from \"{OldValue}\" to \"{NewValue}\"",
        "priority" => $"changed priority from \"{OldValue}\" to \"{NewValue}\"",
        "assigned_to" => $"reassigned task to \"{NewValue}\"",
        "progress" => $"updated progress to {NewValue}%",
        _ => Action
    };
}

public class AttachmentVM
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    public string OriginalName { get; set; } = "";
    public string? FileType { get; set; }
    public string FileSize { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string UploaderName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class TimeLogVM
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public decimal HoursLogged { get; set; }
    public DateTime LogDate { get; set; }
    public string? Description { get; set; }
    public bool IsBillable { get; set; }
    public bool IsApproved { get; set; }
    public string? ApproverName { get; set; }
}

public class NotificationVM
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "";
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo => GetTimeAgo(CreatedAt);

    private static string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.Now - dateTime;
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return dateTime.ToString("MMM d");
    }
}

// =====================================================
// USER MANAGEMENT VIEW MODELS
// =====================================================

public class UserListVM
{
    public List<UserItemVM> Users { get; set; } = new();
    public List<TeamGroupVM> TeamGroups { get; set; } = new();
    public int? RoleId { get; set; }
    public int? DepartmentId { get; set; }
    public int? TeamId { get; set; }
    public bool? IsActive { get; set; }
    public string? Search { get; set; }
    public bool GroupByTeam { get; set; } = false;

    public SelectList? Roles { get; set; }
    public SelectList? Departments { get; set; }
    public SelectList? Teams { get; set; }
}

public class TeamGroupVM
{
    public int? TeamId { get; set; }
    public string TeamName { get; set; } = "No Team";
    public string? DepartmentName { get; set; }
    public string? ManagerName { get; set; }
    public int? ManagerId { get; set; }
    public List<UserItemVM> Members { get; set; } = new();
    public int MemberCount => Members.Count;
}

public class UserItemVM
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? EmployeeId { get; set; }
    public string? JobTitle { get; set; }
    public string? RoleName { get; set; }
    public string? DepartmentName { get; set; }
    public string? TeamName { get; set; }
    public string? ProfileImage { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }
    public int TaskCount { get; set; }
    public int AssignedTasks => TaskCount;
    public string Initials => string.Join("", FullName.Split(' ').Where(s => !string.IsNullOrEmpty(s)).Select(s => s[0].ToString().ToUpper()).Take(2));
}

public class UserFormVM
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = "";

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }

    [Display(Name = "Phone Number")]
    [DataType(DataType.PhoneNumber)]
    [RegularExpression(@"^(\+\d{1,3})?\d{10}$", ErrorMessage = "Phone number must be 10 digits, optionally with country code (e.g., +91)")]
    [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 15 characters")]
    public string? Phone { get; set; }

    [Display(Name = "Employee ID")]
    public string? EmployeeId { get; set; }

    [Display(Name = "Job Title")]
    public string? JobTitle { get; set; }

    [Required]
    [Display(Name = "Role")]
    public int RoleId { get; set; } = 3;

    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [Display(Name = "Team")]
    public int? TeamId { get; set; }

    [Display(Name = "Reports To")]
    public int? ReportsTo { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Hire Date")]
    public DateTime? HireDate { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public SelectList? Roles { get; set; }
    public SelectList? Departments { get; set; }
    public SelectList? Teams { get; set; }
    public SelectList? Managers { get; set; }
}

// =====================================================
// PROJECT MANAGEMENT VIEW MODELS
// =====================================================

public class ProjectListVM
{
    public List<ProjectSummaryVM> Projects { get; set; } = new();
    public string? Status { get; set; }
    public int? DepartmentId { get; set; }
    public string? Priority { get; set; }

    public SelectList? Statuses { get; set; }
    public SelectList? Departments { get; set; }
    public SelectList? Priorities { get; set; }
}

public class ProjectFormVM : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [StringLength(20)]
    public string? Code { get; set; }

    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [Display(Name = "Project Manager")]
    public int? ManagerId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Budget")]
    public decimal Budget { get; set; }

    [Display(Name = "Client Name")]
    public string? ClientName { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public string Status { get; set; } = "planning";
    public string Priority { get; set; } = "medium";

    public SelectList? Departments { get; set; }
    public SelectList? Managers { get; set; }
    public SelectList? Statuses { get; set; }
    public SelectList? Priorities { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Enforce future StartDate for new projects only
        if (Id == 0 && StartDate.HasValue)
        {
            if (StartDate.Value.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Start date cannot be in the past for new projects.",
                    new[] { nameof(StartDate) });
            }
        }

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

public class ProjectDetailsVM
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Code { get; set; }
    public string? DepartmentName { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Budget { get; set; }
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    // Stats
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int OverdueTasks { get; set; }
    public decimal TotalHoursLogged { get; set; }
    public int ProgressPercent => TotalTasks > 0 ? (CompletedTasks * 100 / TotalTasks) : 0;

    // Related
    public List<ProjectMemberVM> Members { get; set; } = new();
    public List<TaskItemVM> Tasks { get; set; } = new();
}

public class ProjectMemberVM
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public string? UserImage { get; set; }
    public string? JobTitle { get; set; }
    public string Role { get; set; } = "";
    public DateTime JoinedAt { get; set; }
}

// =====================================================
// REPORTS VIEW MODELS
// =====================================================

public class TeamReportVM
{
    public string TeamName { get; set; } = "";
    public int TotalMembers { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int OverdueTasks { get; set; }
    public decimal TotalHours { get; set; }
    public int CompletionRate => TotalTasks > 0 ? (CompletedTasks * 100 / TotalTasks) : 0;
    public List<EmployeeSummaryVM> Members { get; set; } = new();
}

public class EmployeeReportVM
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string? JobTitle { get; set; }
    public string? DepartmentName { get; set; }
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int TasksAssigned { get; set; }
    public int TasksCompleted { get; set; }
    public int TasksInProgress { get; set; }
    public int TasksOverdue { get; set; }
    public decimal HoursLogged { get; set; }
    public decimal BillableHours { get; set; }

    public List<TaskItemVM> Tasks { get; set; } = new();
    public List<TimeLogVM> TimeLogs { get; set; } = new();
}

// =====================================================
// REGISTRATION APPROVAL VIEW MODELS
// =====================================================

public class PendingUserListVM
{
    public List<PendingUserItemVM> PendingUsers { get; set; } = new();
    public string? StatusFilter { get; set; }
}

public class PendingUserItemVM
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public string? DepartmentName { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewerName { get; set; }
    public string? RejectionReason { get; set; }
    public string Initials => string.Join("", FullName.Split(' ').Where(s => !string.IsNullOrEmpty(s)).Select(s => s[0].ToString().ToUpper()).Take(2));
}

public class ApproveUserVM
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    [Display(Name = "Phone Number")]
    [DataType(DataType.PhoneNumber)]
    [RegularExpression(@"^(\+\d{1,3})?\d{10}$", ErrorMessage = "Phone number must be 10 digits, optionally with country code (e.g., +91)")]
    [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 15 characters")]
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public int? DepartmentId { get; set; }
    
    [Required]
    [Display(Name = "Assign Role")]
    public int RoleId { get; set; } = 3; // Default to Employee
    
    [Display(Name = "Department")]
    public int? AssignedDepartmentId { get; set; }
    
    [Display(Name = "Team")]
    public int? TeamId { get; set; }
    
    [Display(Name = "Employee ID")]
    public string? EmployeeId { get; set; }
    
    [Display(Name = "Reports To")]
    public int? ReportsTo { get; set; }
    
    [DataType(DataType.Date)]
    [Display(Name = "Hire Date")]
    public DateTime? HireDate { get; set; } = DateTime.Today;
    
    public SelectList? Roles { get; set; }
    public SelectList? Departments { get; set; }
    public SelectList? Teams { get; set; }
    public SelectList? Managers { get; set; }
}

public class RejectUserVM
{
    public int Id { get; set; }
    
    [Required]
    [Display(Name = "Rejection Reason")]
    public string RejectionReason { get; set; } = "";
}


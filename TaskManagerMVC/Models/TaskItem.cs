namespace TaskManagerMVC.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public int PriorityId { get; set; } = 2;
    public int StatusId { get; set; } = 1;
    public DateTime DueDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }
    public int Progress { get; set; }
    public int? AssignedTo { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Enhanced fields for professional task management
    public int? ProjectId { get; set; }
    public int? ParentTaskId { get; set; }
    public string TaskType { get; set; } = "task"; // task, bug, feature, improvement, support
    public int? AssignedBy { get; set; }
    public DateTime? AssignedAt { get; set; }
    public bool IsBillable { get; set; } = false;
    public string? Tags { get; set; }
    public int AttachmentsCount { get; set; }
    public int CommentsCount { get; set; }

    // Navigation properties
    public User? Assignee { get; set; }
    public User? Creator { get; set; }
    public User? Assigner { get; set; }
    public Project? Project { get; set; }
    public TaskItem? ParentTask { get; set; }
    public ICollection<TaskItem>? SubTasks { get; set; }
    public ICollection<TaskComment>? Comments { get; set; }
    public ICollection<TaskAttachment>? Attachments { get; set; }
    public ICollection<TaskActivityLog>? ActivityLogs { get; set; }
    public ICollection<TimeLog>? TimeLogs { get; set; }
    public ICollection<TaskAssignee>? Assignees { get; set; }

    // Computed properties
    public int DaysRemaining => (DueDate.Date - DateTime.Today).Days;
    public bool IsOverdue => DaysRemaining < 0 && StatusId != 7; // Not completed
    public string DueDateStatus => DaysRemaining < 0 ? "overdue" : DaysRemaining == 0 ? "today" : DaysRemaining <= 3 ? "soon" : "normal";
    public decimal ProgressPercent => EstimatedHours > 0 ? Math.Min(100, (ActualHours / EstimatedHours) * 100) : Progress;
}

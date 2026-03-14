using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskManagerMVC.Attributes;

namespace TaskManagerMVC.ViewModels;

public class TaskListVM
{
    public List<TaskItemVM> Tasks { get; set; } = new();

    // Filters
    public int? StatusId { get; set; }
    public int? PriorityId { get; set; }
    public int? CategoryId { get; set; }
    public int? ProjectId { get; set; }

    // Dropdowns
    public SelectList? Categories { get; set; }
    public SelectList? Priorities { get; set; }
    public SelectList? Statuses { get; set; }
}

public class TaskItemVM
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? CategoryName { get; set; }
    public string PriorityName { get; set; } = "";
    public int StatusId { get; set; }
    public string StatusName { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public string? AssigneeName { get; set; }
    public string? ProjectName { get; set; }
}

public class TaskFormVM : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
    public string Title { get; set; } = "";

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Please select a priority")]
    [Display(Name = "Priority")]
    public int PriorityId { get; set; } = 2;

    [Required(ErrorMessage = "Please select a status")]
    [Display(Name = "Status")]
    public int StatusId { get; set; } = 1;

    [Required(ErrorMessage = "Due date is required")]
    [DataType(DataType.Date)]
    [FutureDate(ErrorMessage = "Due date must be today or in the future")]
    [Display(Name = "Due Date")]
    public DateTime? DueDate { get; set; }

    [Display(Name = "Estimated Hours")]
    [Range(0, 1000, ErrorMessage = "Estimated hours must be between 0 and 1000")]
    public decimal EstimatedHours { get; set; }

    // File upload
    [Display(Name = "Attachments")]
    public List<IFormFile>? Attachments { get; set; }

    // Dropdowns
    public SelectList? Categories { get; set; }
    public SelectList? Priorities { get; set; }
    public SelectList? Statuses { get; set; }

    [Display(Name = "Billable")]
    public bool IsBillable { get; set; }

    [Display(Name = "Hourly Rate")]
    [Range(0, 10000, ErrorMessage = "Hourly rate must be between 0 and 10000")]
    public decimal HourlyRate { get; set; }

    public string? Tags { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Validation for Due Date (Custom logic if FutureDate attribute is not enough)
        if (DueDate.HasValue)
        {
           /* if (DueDate.Value.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Due date cannot be in the past.",
                    new[] { nameof(DueDate) });
            }*/
        }
        
        yield break;
    }
}

public class TaskDetailsVM
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? CategoryName { get; set; }
    public string PriorityName { get; set; } = "";
    public string StatusName { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public decimal HourlyRate { get; set; }
    public int? AssignedToId { get; set; }
    public int? ProjectId { get; set; }
    public int? ProjectManagerId { get; set; }
    public List<TaskAttachmentVM> FileAttachments { get; set; } = new();
}

public class TaskAttachmentVM
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    public string OriginalName { get; set; } = "";
    public string? FileType { get; set; }
    public long FileSize { get; set; }
    public string FilePath { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? UploadedByName { get; set; }

    public string FileSizeFormatted => FileSize switch
    {
        < 1024 => $"{FileSize} B",
        < 1048576 => $"{FileSize / 1024.0:F1} KB",
        _ => $"{FileSize / 1048576.0:F1} MB"
    };

    public string FileIcon => FileType?.ToLower() switch
    {
        "pdf" => "bi-file-earmark-pdf text-danger",
        "doc" or "docx" => "bi-file-earmark-word text-primary",
        "xls" or "xlsx" => "bi-file-earmark-excel text-success",
        "ppt" or "pptx" => "bi-file-earmark-ppt text-warning",
        "png" or "jpg" or "jpeg" or "gif" or "svg" => "bi-file-earmark-image text-info",
        "zip" or "rar" or "7z" => "bi-file-earmark-zip text-secondary",
        _ => "bi-file-earmark text-muted"
    };
}

public class DashboardVM
{
    public string UserName { get; set; } = "";
    public int TotalTasks { get; set; }
    public int PendingTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int CompletedTasks { get; set; }
    public List<TaskItemVM> OverdueTasks { get; set; } = new();
    public List<TaskItemVM> RecentTasks { get; set; } = new();
}

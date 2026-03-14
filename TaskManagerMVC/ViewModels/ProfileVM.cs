using System.ComponentModel.DataAnnotations;

namespace TaskManagerMVC.ViewModels;

public class ProfileVM
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public string? DepartmentName { get; set; }
    public string? TeamName { get; set; }
    public string? ManagerName { get; set; }
    public string? EmployeeId { get; set; }
    public DateTime? HireDate { get; set; }
    public string? RoleName { get; set; }
    public string? ProfileImage { get; set; }

    // Stats
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingTasks { get; set; }
    public decimal TotalHoursLogged { get; set; }
    
    public string Initials => string.Join("", FullName.Split(' ').Where(s => !string.IsNullOrEmpty(s)).Select(s => s[0].ToString().ToUpper()).Take(2));
}

public class SettingsVM
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Current password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "New password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string NewPassword { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmNewPassword { get; set; } = "";
    
    // Future settings can go here (Notifications, Theme, etc.)
    public bool EmailNotifications { get; set; } = true;
}

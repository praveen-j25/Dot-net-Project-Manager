namespace TaskManagerMVC.Models;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastLogin { get; set; }

    // Role-based access
    public int RoleId { get; set; } = 3; // Default: Employee
    public int? DepartmentId { get; set; }
    public int? TeamId { get; set; }
    public string? JobTitle { get; set; }
    public string? EmployeeId { get; set; }
    public string? ProfileImage { get; set; }
    public int? ReportsTo { get; set; }
    public DateTime? HireDate { get; set; }

    // Navigation properties
    public Role? Role { get; set; }
    public Department? Department { get; set; }
    public Team? Team { get; set; }
    public User? Manager { get; set; }
    public ICollection<User>? DirectReports { get; set; }
    public ICollection<TaskItem>? AssignedTasks { get; set; }
    public ICollection<TaskItem>? CreatedTasks { get; set; }
    public ICollection<Notification>? Notifications { get; set; }

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
    public string Initials => $"{FirstName?.FirstOrDefault()}{LastName?.FirstOrDefault()}".ToUpper();
    public bool IsAdmin => RoleId == Role.AdminRoleId;
    public bool IsManager => RoleId == Role.ManagerRoleId;
    public bool IsEmployee => RoleId == Role.EmployeeRoleId;
}

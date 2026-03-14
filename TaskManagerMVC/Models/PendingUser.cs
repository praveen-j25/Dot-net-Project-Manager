namespace TaskManagerMVC.Models;

public class PendingUser
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Phone { get; set; }
    public int? DepartmentId { get; set; }
    public string? JobTitle { get; set; }
    
    // Request details
    public string Status { get; set; } = "pending"; // pending, approved, rejected
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
    
    // Navigation
    public string FullName => $"{FirstName} {LastName}";
    public Department? Department { get; set; }
    public User? Reviewer { get; set; }
}

namespace TaskManagerMVC.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Color { get; set; } = "#6c757d";
    public string Icon { get; set; } = "bi-folder";
    public bool IsActive { get; set; } = true;
}

public class Priority
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public string Color { get; set; } = "";
}

public class Status
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Color { get; set; } = "";
    public int SortOrder { get; set; }
}

public class PasswordReset
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ResetToken { get; set; } = "";
    public DateTime TokenExpiry { get; set; }
    public bool IsUsed { get; set; }
}

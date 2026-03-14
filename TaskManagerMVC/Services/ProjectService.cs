using MySql.Data.MySqlClient;
using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskManagerMVC.Data;
using TaskManagerMVC.Models;
using TaskManagerMVC.ViewModels;

namespace TaskManagerMVC.Services;

public class ProjectService
{
    private readonly DbConnectionFactory _dbFactory;

    public ProjectService(DbConnectionFactory dbFactory) => _dbFactory = dbFactory;

    /// <summary>
    /// Get all projects with filters
    /// </summary>
    public async Task<ProjectListVM> GetProjectsAsync(string? status = null, int? departmentId = null, string? priority = null)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetProjectsWithFilters", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_status", (object?)status ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_department_id", (object?)departmentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_priority", (object?)priority ?? DBNull.Value);

        var projects = new List<ProjectSummaryVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            projects.Add(new ProjectSummaryVM
            {
                Id = Convert.ToInt32(reader["id"]),
                Name = reader["name"].ToString()!,
                Code = reader["code"] == DBNull.Value ? null : reader["code"].ToString()!,
                Status = reader["status"].ToString()!,
                Priority = reader["priority"].ToString()!,
                ManagerName = reader["manager_name"] == DBNull.Value ? null : reader["manager_name"].ToString()!,
                StartDate = reader["start_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["start_date"]),
                EndDate = reader["end_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["end_date"]),
                TotalTasks = Convert.ToInt32(reader["total_tasks"]),
                CompletedTasks = Convert.ToInt32(reader["completed_tasks"]),
                TeamSize = Convert.ToInt32(reader["team_size"])
            });
        }

        return new ProjectListVM
        {
            Projects = projects,
            Status = status,
            DepartmentId = departmentId,
            Priority = priority,
            Statuses = GetProjectStatusSelectList(),
            Departments = new SelectList(await GetDepartmentsAsync(), "Id", "Name"),
            Priorities = GetPrioritySelectList()
        };
    }

    /// <summary>
    /// Get project form for create/edit
    /// </summary>
    public async Task<ProjectFormVM> GetProjectFormAsync(int? id = null)
    {
        var vm = new ProjectFormVM
        {
            Departments = new SelectList(await GetDepartmentsAsync(), "Id", "Name"),
            Managers = new SelectList(await GetManagersAsync(), "Id", "FullName"),
            Statuses = GetProjectStatusSelectList(),
            Priorities = GetPrioritySelectList()
        };

        if (id.HasValue)
        {
            using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("sp_GetProjectForEdit", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_id", id.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                vm.Id = Convert.ToInt32(reader["id"]);
                vm.Name = reader["name"].ToString()!;
                vm.Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString()!;
                vm.Code = reader["code"] == DBNull.Value ? null : reader["code"].ToString()!;
                vm.DepartmentId = reader["department_id"] == DBNull.Value ? null : Convert.ToInt32(reader["department_id"]);
                vm.ManagerId = reader["manager_id"] == DBNull.Value ? null : Convert.ToInt32(reader["manager_id"]);
                vm.StartDate = reader["start_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["start_date"]);
                vm.EndDate = reader["end_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["end_date"]);
                vm.Budget = Convert.ToDecimal(reader["budget"]);
                vm.Status = reader["status"].ToString()!;
                vm.Priority = reader["priority"].ToString()!;
            }
        }

        return vm;
    }

    /// <summary>
    /// Get project details with tasks and members
    /// </summary>
    public async Task<ProjectDetailsVM?> GetProjectDetailsAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetProjectDetails", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_id", id);

        ProjectDetailsVM? project = null;
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                project = new ProjectDetailsVM
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Name = reader["name"].ToString()!,
                    Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString()!,
                    Code = reader["code"] == DBNull.Value ? null : reader["code"].ToString()!,
                    DepartmentName = reader["department_name"] == DBNull.Value ? null : reader["department_name"].ToString()!,
                    ManagerId = reader["manager_id"] == DBNull.Value ? null : Convert.ToInt32(reader["manager_id"]),
                    ManagerName = reader["manager_name"] == DBNull.Value ? null : reader["manager_name"].ToString()!,
                    StartDate = reader["start_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["start_date"]),
                    EndDate = reader["end_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["end_date"]),
                    Budget = Convert.ToDecimal(reader["budget"]),
                    Status = reader["status"].ToString()!,
                    Priority = reader["priority"].ToString()!,
                    CreatedAt = Convert.ToDateTime(reader["created_at"])
                };
            }
        }

        if (project == null) return null;

        // Get task statistics
        using (var statsCmd = new MySqlCommand("sp_GetProjectTaskStats", conn))
        {
            statsCmd.CommandType = CommandType.StoredProcedure;
            statsCmd.Parameters.AddWithValue("p_project_id", id);
            using var reader = await statsCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                project.TotalTasks = Convert.ToInt32(reader["total"]);
                project.CompletedTasks = reader["completed"] == DBNull.Value ? 0 : Convert.ToInt32(reader["completed"]);
                project.InProgressTasks = reader["in_progress"] == DBNull.Value ? 0 : Convert.ToInt32(reader["in_progress"]);
                project.OverdueTasks = reader["overdue"] == DBNull.Value ? 0 : Convert.ToInt32(reader["overdue"]);
            }
        }

        // Get total hours logged - table may not exist
        try
        {
            using var hoursCmd = new MySqlCommand("sp_GetProjectTotalHours", conn);
            hoursCmd.CommandType = CommandType.StoredProcedure;
            hoursCmd.Parameters.AddWithValue("p_project_id", id);
            using var reader = await hoursCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                project.TotalHoursLogged = Convert.ToDecimal(reader["total_hours"]);
            }
        }
        catch
        {
            project.TotalHoursLogged = 0;
        }

        // Project members - table may not exist, so skip for now
        // Members will be empty until project_members table is created

        // Get project tasks
        using (var tasksCmd = new MySqlCommand("sp_GetProjectTasks", conn))
        {
            tasksCmd.CommandType = CommandType.StoredProcedure;
            tasksCmd.Parameters.AddWithValue("p_project_id", id);
            using var reader = await tasksCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                project.Tasks.Add(new TaskItemVM
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Title = reader["title"].ToString()!,
                    Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString()!,
                    DueDate = reader["due_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["due_date"]),
                    PriorityName = reader["priority_name"].ToString()!,
                    StatusName = reader["status_name"].ToString()!,
                    CategoryName = reader["category_name"] == DBNull.Value ? null : reader["category_name"].ToString()!
                });
            }
        }

        return project;
    }

    /// <summary>
    /// Create new project
    /// </summary>
    public async Task<int> CreateProjectAsync(int userId, ProjectFormVM model)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        // Auto-generate Project Code if not provided
        if (string.IsNullOrEmpty(model.Code))
        {
            model.Code = await GenerateProjectCodeAsync(conn);
        }
        else
        {
            // Check if the manually entered code already exists
            if (await IsProjectCodeExistsAsync(conn, model.Code, model.Id))
            {
                throw new InvalidOperationException($"Project code '{model.Code}' already exists. Please use a different code.");
            }
        }

        using var cmd = new MySqlCommand("sp_CreateProject", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_name", model.Name);
        cmd.Parameters.AddWithValue("p_description", (object?)model.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_code", model.Code);
        cmd.Parameters.AddWithValue("p_department_id", (object?)model.DepartmentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_manager_id", (object?)model.ManagerId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_start_date", (object?)model.StartDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_end_date", (object?)model.EndDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_budget", model.Budget);
        cmd.Parameters.AddWithValue("p_status", model.Status);
        cmd.Parameters.AddWithValue("p_priority", model.Priority);
        cmd.Parameters.AddWithValue("p_created_by", userId);
        cmd.Parameters.AddWithValue("p_created_at", DateTime.Now);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Convert.ToInt32(reader["id"]);
        }
        return 0;
    }

    /// <summary>
    /// Check if project code already exists
    /// </summary>
    private async Task<bool> IsProjectCodeExistsAsync(MySqlConnection conn, string code, int? excludeProjectId = null)
    {
        using var cmd = new MySqlCommand("sp_CheckProjectCodeExists", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_code", code);
        cmd.Parameters.AddWithValue("p_exclude_id", (object?)excludeProjectId ?? DBNull.Value);
        
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Convert.ToInt32(reader["count"]) > 0;
        }
        return false;
    }

    /// <summary>
    /// Generate unique Project Code
    /// </summary>
    private async Task<string> GenerateProjectCodeAsync(MySqlConnection conn)
    {
        using var cmd = new MySqlCommand("sp_GetNextProjectCode", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var lastCode = reader["code"] == DBNull.Value ? null : reader["code"].ToString();
            
            int nextNumber = 1;
            if (lastCode != null && lastCode.StartsWith("PRJ"))
            {
                var numberPart = lastCode.Substring(3);
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }
            
            return $"PRJ{nextNumber:D3}"; // PRJ001, PRJ002, etc.
        }
        
        return "PRJ001";
    }

    /// <summary>
    /// Generate next Project Code (public method for API)
    /// </summary>
    public async Task<string> GenerateNextProjectCodeAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        return await GenerateProjectCodeAsync(conn);
    }

    /// <summary>
    /// Update existing project
    /// </summary>
    public async Task UpdateProjectAsync(ProjectFormVM model)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        // Check if the code already exists for another project
        if (!string.IsNullOrEmpty(model.Code) && await IsProjectCodeExistsAsync(conn, model.Code, model.Id))
        {
            throw new InvalidOperationException($"Project code '{model.Code}' already exists. Please use a different code.");
        }

        // SECURITY: Fetch existing project to ensure StartDate is not modified (prevent backdating/tampering)
        var existingProject = await GetProjectFormAsync(model.Id);
        
        using var cmd = new MySqlCommand("sp_UpdateProject", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_id", model.Id);
        cmd.Parameters.AddWithValue("p_name", model.Name);
        cmd.Parameters.AddWithValue("p_description", (object?)model.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_code", (object?)model.Code ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_department_id", (object?)model.DepartmentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_manager_id", (object?)model.ManagerId ?? DBNull.Value);
        // Force original StartDate
        cmd.Parameters.AddWithValue("p_start_date", (object?)existingProject.StartDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_end_date", (object?)model.EndDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_budget", model.Budget);
        cmd.Parameters.AddWithValue("p_status", model.Status);
        cmd.Parameters.AddWithValue("p_priority", model.Priority);
        cmd.Parameters.AddWithValue("p_updated_at", DateTime.Now);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Delete project (soft delete by setting is_active = false)
    /// </summary>
    public async Task DeleteProjectAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_DeleteProject", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_id", id);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Add member to project - disabled until project_members table is created
    /// </summary>
    public Task AddProjectMemberAsync(int projectId, int userId, string role = "member")
    {
        // project_members table doesn't exist yet
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get projects for dropdown
    /// </summary>
    public async Task<List<Project>> GetProjectsForSelectAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetProjectsForSelect", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        var projects = new List<Project>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            projects.Add(new Project { Id = Convert.ToInt32(reader["id"]), Name = reader["name"].ToString()! });
        return projects;
    }

    // Helper methods
    private async Task<List<Department>> GetDepartmentsAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("sp_GetDepartments", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        var departments = new List<Department>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            departments.Add(new Department { Id = Convert.ToInt32(reader["id"]), Name = reader["name"].ToString()! });
        return departments;
    }

    private async Task<List<User>> GetManagersAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("sp_GetProjectManagers", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        var managers = new List<User>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            managers.Add(new User { Id = Convert.ToInt32(reader["id"]), FirstName = reader["full_name"].ToString()!, LastName = "" });
        return managers;
    }

    private SelectList GetProjectStatusSelectList()
    {
        var statuses = new List<object>
        {
            new { Value = "planning", Text = "Planning" },
            new { Value = "active", Text = "Active" },
            new { Value = "on_hold", Text = "On Hold" },
            new { Value = "completed", Text = "Completed" },
            new { Value = "cancelled", Text = "Cancelled" }
        };
        return new SelectList(statuses, "Value", "Text");
    }

    private SelectList GetPrioritySelectList()
    {
        var priorities = new List<object>
        {
            new { Value = "low", Text = "Low" },
            new { Value = "medium", Text = "Medium" },
            new { Value = "high", Text = "High" },
            new { Value = "critical", Text = "Critical" }
        };
        return new SelectList(priorities, "Value", "Text");
    }
}

using System.Data;
using MySql.Data.MySqlClient;
using TaskManagerMVC.Data;
using TaskManagerMVC.ViewModels;

namespace TaskManagerMVC.Services;

public class ManagerService
{
    private readonly DbConnectionFactory _dbFactory;

    public ManagerService(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Get manager dashboard data
    /// </summary>
    public async Task<ManagerDashboardVM> GetManagerDashboardAsync(int managerId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        var dashboard = new ManagerDashboardVM();

        // Get manager's department
        var deptId = await GetManagerDepartmentAsync(conn, managerId);

        // Team statistics
        dashboard.TotalTeamMembers = await GetTeamMemberCountAsync(conn, deptId);
        dashboard.ActiveToday = await GetActiveTodayCountAsync(conn, deptId);
        dashboard.OnLeave = 0; // TODO: Implement leave tracking

        // Project statistics
        dashboard.ActiveProjects = await GetActiveProjectsCountAsync(conn, managerId);
        dashboard.ProjectsOnTrack = await GetProjectsOnTrackCountAsync(conn, managerId);
        dashboard.ProjectsAtRisk = await GetProjectsAtRiskCountAsync(conn, managerId);
        dashboard.ProjectsDelayed = await GetProjectsDelayedCountAsync(conn, managerId);

        // Task statistics
        dashboard.TotalTasks = await GetTotalTasksCountAsync(conn, managerId);
        dashboard.CompletedThisWeek = await GetCompletedThisWeekCountAsync(conn, managerId);
        dashboard.OverdueTasks = await GetOverdueTasksCountAsync(conn, managerId);
        dashboard.InProgressTasks = await GetInProgressTasksCountAsync(conn, managerId);

        // Performance metrics
        dashboard.TeamCompletionRate = await GetTeamCompletionRateAsync(conn, deptId);
        dashboard.AverageTaskTime = await GetAverageTaskTimeAsync(conn, deptId);

        // Recent activities
        dashboard.RecentActivities = await GetRecentActivitiesAsync(conn, managerId, 10);

        // Top performers
        dashboard.TopPerformers = await GetTopPerformersAsync(conn, deptId, 5);

        // Upcoming deadlines
        dashboard.UpcomingDeadlines = await GetUpcomingDeadlinesAsync(conn, managerId, 5);

        return dashboard;
    }

    /// <summary>
    /// Get team members under this manager
    /// </summary>
    public async Task<TeamMembersVM> GetTeamMembersAsync(int managerId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        var deptId = await GetManagerDepartmentAsync(conn, managerId);
        var deptName = await GetDepartmentNameAsync(conn, deptId);

        using var cmd = new MySqlCommand("sp_GetTeamMembers", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_dept_id", (object?)deptId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_manager_id", managerId);

        var members = new List<TeamMemberVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            members.Add(new TeamMemberVM
            {
                Id = Convert.ToInt32(reader["id"]),
                FirstName = reader["first_name"].ToString()!,
                LastName = reader["last_name"].ToString()!,
                Email = reader["email"].ToString()!,
                EmployeeId = reader["employee_id"].ToString()!,
                RoleName = reader["role_name"].ToString()!,
                DepartmentName = reader["department_name"]?.ToString(),
                TeamName = reader["team_name"]?.ToString(),
                IsActive = Convert.ToBoolean(reader["is_active"]),
                ActiveTasks = Convert.ToInt32(reader["active_tasks"]),
                CompletedTasks = Convert.ToInt32(reader["completed_tasks"])
            });
        }

        return new TeamMembersVM 
        { 
            Members = members,
            DepartmentId = deptId,
            DepartmentName = deptName
        };
    }

    /// <summary>
    /// Get projects managed by this manager
    /// </summary>
    public async Task<ManagerProjectsVM> GetManagedProjectsAsync(int managerId, string? status)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetManagedProjects", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_manager_id", managerId);
        cmd.Parameters.AddWithValue("p_status", (object?)status ?? DBNull.Value);

        var projects = new List<ManagerProjectVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            projects.Add(new ManagerProjectVM
            {
                Id = Convert.ToInt32(reader["id"]),
                Name = reader["name"].ToString()!,
                Code = reader["code"]?.ToString(),
                Description = reader["description"]?.ToString(),
                Status = reader["status"].ToString()!,
                Priority = reader["priority"].ToString()!,
                StartDate = reader["start_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["start_date"]),
                EndDate = reader["end_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["end_date"]),
                Budget = Convert.ToDecimal(reader["budget"]),
                DepartmentName = reader["department_name"]?.ToString(),
                TotalTasks = Convert.ToInt32(reader["total_tasks"]),
                CompletedTasks = Convert.ToInt32(reader["completed_tasks"])
            });
        }

        return new ManagerProjectsVM 
        { 
            Projects = projects,
            StatusFilter = status
        };
    }

    /// <summary>
    /// Get team performance data
    /// </summary>
    public async Task<TeamPerformanceVM> GetTeamPerformanceAsync(int managerId, DateTime? startDate, DateTime? endDate)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        var deptId = await GetManagerDepartmentAsync(conn, managerId);

        // Set default date range if not provided
        startDate ??= DateTime.Today.AddDays(-30);
        endDate ??= DateTime.Today;

        // Validate date range
        if (startDate > endDate)
        {
            // Swap dates if start is after end
            (startDate, endDate) = (endDate, startDate);
        }

        using var cmd = new MySqlCommand("sp_GetTeamPerformance", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_dept_id", (object?)deptId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_manager_id", managerId);
        cmd.Parameters.AddWithValue("p_start_date", startDate);
        cmd.Parameters.AddWithValue("p_end_date", endDate);

        var memberPerformance = new List<TeamMemberPerformanceVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var tasksAssigned = Convert.ToInt32(reader["tasks_assigned"]);
            var tasksCompleted = Convert.ToInt32(reader["tasks_completed"]);

            memberPerformance.Add(new TeamMemberPerformanceVM
            {
                UserId = Convert.ToInt32(reader["id"]),
                UserName = $"{reader["first_name"]} {reader["last_name"]}",
                EmployeeId = reader["employee_id"].ToString()!,
                TasksAssigned = tasksAssigned,
                TasksCompleted = tasksCompleted,
                TasksInProgress = Convert.ToInt32(reader["tasks_in_progress"]),
                TasksOverdue = Convert.ToInt32(reader["tasks_overdue"]),
                CompletionRate = tasksAssigned > 0 ? (decimal)tasksCompleted / tasksAssigned * 100 : 0
            });
        }

        var totalAssigned = memberPerformance.Sum(m => m.TasksAssigned);
        var totalCompleted = memberPerformance.Sum(m => m.TasksCompleted);

        return new TeamPerformanceVM
        {
            StartDate = startDate,
            EndDate = endDate,
            MemberPerformance = memberPerformance,
            OverallCompletionRate = totalAssigned > 0 ? (decimal)totalCompleted / totalAssigned * 100 : 0,
            TotalTasksCompleted = totalCompleted,
            TotalTasksOverdue = memberPerformance.Sum(m => m.TasksOverdue)
        };
    }

    // ============================================
    // HELPER METHODS
    // ============================================

    private async Task<int?> GetManagerDepartmentAsync(MySqlConnection conn, int managerId)
    {
        using var cmd = new MySqlCommand("sp_GetManagerDepartment", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_manager_id", managerId);
        var result = await cmd.ExecuteScalarAsync();
        return result == DBNull.Value ? null : Convert.ToInt32(result);
    }

    private async Task<string?> GetDepartmentNameAsync(MySqlConnection conn, int? deptId)
    {
        if (!deptId.HasValue) return null;
        
        using var cmd = new MySqlCommand("sp_GetDepartmentName", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_dept_id", deptId);
        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString();
    }

    private async Task<int> GetTeamMemberCountAsync(MySqlConnection conn, int? deptId)
    {
        using var cmd = new MySqlCommand("sp_GetTeamMemberCount", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_dept_id", (object?)deptId ?? DBNull.Value);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<int> GetActiveTodayCountAsync(MySqlConnection conn, int? deptId)
    {
        // For now, return all active users. Can be enhanced with login tracking
        return await GetTeamMemberCountAsync(conn, deptId);
    }

    private async Task<int> GetActiveProjectsCountAsync(MySqlConnection conn, int managerId)
    {
        using var cmd = new MySqlCommand("sp_GetManagerActiveProjectsCount", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_manager_id", managerId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<int> GetProjectsOnTrackCountAsync(MySqlConnection conn, int managerId)
    {
        using var cmd = new MySqlCommand("sp_GetProjectsOnTrackCount", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_manager_id", managerId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<int> GetProjectsAtRiskCountAsync(MySqlConnection conn, int managerId)
    {
        using var cmd = new MySqlCommand("sp_GetProjectsAtRiskCount", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_manager_id", managerId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<int> GetProjectsDelayedCountAsync(MySqlConnection conn, int managerId)
    {
        using var cmd = new MySqlCommand("sp_GetProjectsDelayedCount", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_manager_id", managerId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<int> GetTotalTasksCountAsync(MySqlConnection conn, int managerId)
    {
        using var cmd = new MySqlCommand("sp_GetTotalTasksCount", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_manager_id", managerId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<int> GetCompletedThisWeekCountAsync(MySqlConnection conn, int managerId)
    {
        using var cmd = new MySqlCommand("sp_GetCompletedThisWeekCount", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_manager_id", managerId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<int> GetOverdueTasksCountAsync(MySqlConnection conn, int managerId)
    {
        using var cmd = new MySqlCommand("sp_GetOverdueTasksCount", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_manager_id", managerId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<int> GetInProgressTasksCountAsync(MySqlConnection conn, int managerId)
    {
        using var cmd = new MySqlCommand("sp_GetInProgressTasksCount", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_manager_id", managerId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<decimal> GetTeamCompletionRateAsync(MySqlConnection conn, int? deptId)
    {
        using var cmd = new MySqlCommand("sp_GetTeamCompletionRate", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_dept_id", (object?)deptId ?? DBNull.Value);
        var result = await cmd.ExecuteScalarAsync();
        return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
    }

    private async Task<decimal> GetAverageTaskTimeAsync(MySqlConnection conn, int? deptId)
    {
        using var cmd = new MySqlCommand("sp_GetAverageTaskTime", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_dept_id", (object?)deptId ?? DBNull.Value);
        var result = await cmd.ExecuteScalarAsync();
        return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
    }

    private async Task<List<ActivityVM>> GetRecentActivitiesAsync(MySqlConnection conn, int managerId, int limit)
    {
        using var cmd = new MySqlCommand("sp_GetManagerRecentActivities", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_manager_id", managerId);
        cmd.Parameters.AddWithValue("p_limit", limit);

        var activities = new List<ActivityVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            activities.Add(new ActivityVM
            {
                UserName = reader["user_name"].ToString()!,
                Action = reader["action"].ToString()!,
                EntityType = reader["entity_type"].ToString()!,
                EntityName = reader["entity_name"].ToString()!,
                CreatedAt = Convert.ToDateTime(reader["created_at"])
            });
        }

        return activities;
    }

    private async Task<List<TopPerformerVM>> GetTopPerformersAsync(MySqlConnection conn, int? deptId, int limit)
    {
        var sql = @"
            SELECT 
                u.id as user_id,
                CONCAT(u.first_name, ' ', u.last_name) as user_name,
                u.employee_id,
                COUNT(t.id) as total_tasks,
                SUM(CASE WHEN t.status_id = 4 THEN 1 ELSE 0 END) as completed_tasks,
                SUM(CASE WHEN t.status_id = 4 AND t.updated_at <= t.due_date THEN 1 ELSE 0 END) as on_time_tasks
            FROM users u
            LEFT JOIN tasks t ON u.id = t.assigned_to 
                AND t.created_at >= DATE_SUB(CURDATE(), INTERVAL 30 DAY)
            WHERE (@deptId IS NULL OR u.department_id = @deptId)
              AND u.is_active = 1
            GROUP BY u.id, u.first_name, u.last_name, u.employee_id
            HAVING total_tasks > 0
            ORDER BY completed_tasks DESC, on_time_tasks DESC
            LIMIT @limit";

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@deptId", (object?)deptId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@limit", limit);

        var performers = new List<TopPerformerVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var totalTasks = Convert.ToInt32(reader["total_tasks"]);
            var completedTasks = Convert.ToInt32(reader["completed_tasks"]);

            performers.Add(new TopPerformerVM
            {
                UserId = Convert.ToInt32(reader["user_id"]),
                UserName = reader["user_name"].ToString()!,
                EmployeeId = reader["employee_id"].ToString()!,
                CompletedTasks = completedTasks,
                CompletionRate = totalTasks > 0 ? (decimal)completedTasks / totalTasks * 100 : 0,
                OnTimeTasks = Convert.ToInt32(reader["on_time_tasks"])
            });
        }

        return performers;
    }

    private async Task<List<UpcomingDeadlineVM>> GetUpcomingDeadlinesAsync(MySqlConnection conn, int managerId, int limit)
    {
        using var cmd = new MySqlCommand("sp_GetUpcomingDeadlines", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_manager_id", managerId);
        cmd.Parameters.AddWithValue("p_limit", limit);

        var deadlines = new List<UpcomingDeadlineVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            deadlines.Add(new UpcomingDeadlineVM
            {
                TaskId = Convert.ToInt32(reader["task_id"]),
                TaskTitle = reader["task_title"].ToString()!,
                ProjectName = reader["project_name"].ToString()!,
                AssigneeName = reader["assignee_name"]?.ToString() ?? "Unassigned",
                DueDate = Convert.ToDateTime(reader["due_date"]),
                Priority = reader["priority"]?.ToString() ?? "Medium"
            });
        }

        return deadlines;
    }
}

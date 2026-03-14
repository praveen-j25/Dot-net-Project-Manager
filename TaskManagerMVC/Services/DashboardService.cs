using MySql.Data.MySqlClient;
using System.Data;
using TaskManagerMVC.Data;
using TaskManagerMVC.ViewModels;

namespace TaskManagerMVC.Services;

public class DashboardService
{
    private readonly DbConnectionFactory _dbFactory;

    public DashboardService(DbConnectionFactory dbFactory) => _dbFactory = dbFactory;

    /// <summary>
    /// Get admin dashboard data - Uses stored procedures only
    /// </summary>
    public async Task<AdminDashboardVM> GetAdminDashboardAsync(string userName)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        var dashboard = new AdminDashboardVM { UserName = userName };

        // Get employee count
        using (var cmd = new MySqlCommand("sp_GetActiveEmployeeCount", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                dashboard.TotalEmployees = Convert.ToInt32(reader["count"]);
            }
        }

        // Get active projects count
        using (var cmd = new MySqlCommand("sp_GetActiveProjectsCount", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                dashboard.ActiveProjects = Convert.ToInt32(reader["count"]);
            }
        }

        // Get task statistics
        using (var cmd = new MySqlCommand("sp_GetTaskStatistics", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                dashboard.TotalTasks = reader["total"] == DBNull.Value ? 0 : Convert.ToInt32(reader["total"]);
                dashboard.CompletedTasks = reader["completed"] == DBNull.Value ? 0 : Convert.ToInt32(reader["completed"]);
                dashboard.PendingTasks = reader["pending"] == DBNull.Value ? 0 : Convert.ToInt32(reader["pending"]);
                dashboard.InProgressTasks = reader["in_progress"] == DBNull.Value ? 0 : Convert.ToInt32(reader["in_progress"]);
                dashboard.InReviewTasks = reader["in_review"] == DBNull.Value ? 0 : Convert.ToInt32(reader["in_review"]);
                dashboard.OverdueTasks = reader["overdue"] == DBNull.Value ? 0 : Convert.ToInt32(reader["overdue"]);
            }
        }

        // Get total hours logged
        try
        {
            using var cmd = new MySqlCommand("sp_GetTotalHoursLogged", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                dashboard.TotalHoursLogged = reader["total_hours"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["total_hours"]);
            }
        }
        catch
        {
            dashboard.TotalHoursLogged = 0;
        }

        // Get tasks by status
        try
        {
            using var cmd = new MySqlCommand("sp_GetTasksByStatus", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var statusName = reader["name"]?.ToString() ?? "Unknown";
                var count = reader["count"] == DBNull.Value ? 0 : Convert.ToInt32(reader["count"]);
                dashboard.TasksByStatus[statusName] = count;
            }
        }
        catch { }

        // Get tasks by priority
        try
        {
            using var cmd = new MySqlCommand("sp_GetTasksByPriority", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var priorityName = reader["name"]?.ToString() ?? "Unknown";
                var count = reader["count"] == DBNull.Value ? 0 : Convert.ToInt32(reader["count"]);
                dashboard.TasksByPriority[priorityName] = count;
            }
        }
        catch { }

        // Get tasks by department
        using (var cmd = new MySqlCommand("sp_GetTasksByDepartment", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var deptName = reader["name"]?.ToString() ?? "Unknown";
                var count = reader["count"] == DBNull.Value ? 0 : Convert.ToInt32(reader["count"]);
                dashboard.TasksByDepartment[deptName] = count;
            }
        }

        // Get monthly trends
        using (var cmd = new MySqlCommand("sp_GetMonthlyTaskTrends", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var monthStr = reader["month"]?.ToString() ?? DateTime.Now.ToString("yyyy-MM");
                var created = reader["created"] == DBNull.Value ? 0 : Convert.ToInt32(reader["created"]);
                var completed = reader["completed"] == DBNull.Value ? 0 : Convert.ToInt32(reader["completed"]);
                
                dashboard.MonthlyTrends.Add(new MonthlyTaskData
                {
                    Month = DateTime.ParseExact(monthStr, "yyyy-MM", null).ToString("MMM"),
                    Created = created,
                    Completed = completed
                });
            }
        }

        // Get top performers
        using (var cmd = new MySqlCommand("sp_GetTopPerformers", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var firstName = reader["first_name"]?.ToString() ?? "";
                var lastName = reader["last_name"]?.ToString() ?? "";
                var totalTasks = reader["total"] == DBNull.Value ? 0 : Convert.ToInt32(reader["total"]);
                var completedTasks = reader["completed"] == DBNull.Value ? 0 : Convert.ToInt32(reader["completed"]);
                
                dashboard.TopPerformers.Add(new EmployeeSummaryVM
                {
                    Id = Convert.ToInt32(reader["id"]),
                    FullName = $"{firstName} {lastName}",
                    ProfileImage = reader["profile_image"] == DBNull.Value ? null : reader["profile_image"].ToString()!,
                    JobTitle = reader["job_title"] == DBNull.Value ? null : reader["job_title"].ToString()!,
                    DepartmentName = reader["department_name"] == DBNull.Value ? null : reader["department_name"].ToString()!,
                    TotalTasks = totalTasks,
                    CompletedTasks = completedTasks
                });
            }
        }

        // Get active projects
        using (var cmd = new MySqlCommand("sp_GetActiveProjectsList", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dashboard.ActiveProjectsList.Add(new ProjectSummaryVM
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Name = reader["name"].ToString()!,
                    Code = reader["code"] == DBNull.Value ? null : reader["code"].ToString()!,
                    Status = reader["status"].ToString()!,
                    Priority = reader["priority"].ToString()!,
                    ManagerName = reader["manager_name"] == DBNull.Value ? null : reader["manager_name"].ToString()!,
                    EndDate = reader["end_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["end_date"]),
                    TotalTasks = Convert.ToInt32(reader["total_tasks"]),
                    CompletedTasks = Convert.ToInt32(reader["completed_tasks"])
                });
            }
        }

        // Get recent tasks
        using (var cmd = new MySqlCommand("sp_GetRecentTasks", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dashboard.RecentTasks.Add(new TaskItemVM
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Title = reader["title"].ToString()!,
                    DueDate = reader["due_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["due_date"]),
                    PriorityName = reader["priority_name"].ToString()!,
                    StatusName = reader["status_name"].ToString()!
                });
            }
        }

        // Get overdue tasks
        using (var cmd = new MySqlCommand("sp_GetOverdueTasksList", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dashboard.OverdueTasksList.Add(new TaskItemVM
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Title = reader["title"].ToString()!,
                    DueDate = Convert.ToDateTime(reader["due_date"]),
                    PriorityName = reader["priority_name"].ToString()!,
                    StatusName = reader["status_name"].ToString()!
                });
            }
        }

        return dashboard;
    }

    /// <summary>
    /// Get employee dashboard data - Uses stored procedures only
    /// </summary>
    public async Task<EmployeeDashboardVM> GetEmployeeDashboardAsync(int userId, string userName)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        var dashboard = new EmployeeDashboardVM { UserName = userName };

        // Get user details
        using (var cmd = new MySqlCommand("sp_GetEmployeeDetails", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_user_id", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                dashboard.JobTitle = reader["job_title"] == DBNull.Value ? null : reader["job_title"].ToString()!;
                dashboard.DepartmentName = reader["department_name"] == DBNull.Value ? null : reader["department_name"].ToString()!;
                dashboard.TeamName = reader["team_name"] == DBNull.Value ? null : reader["team_name"].ToString()!;
            }
        }

        // Get task statistics
        using (var cmd = new MySqlCommand("sp_GetEmployeeTaskStatistics", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_user_id", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                dashboard.MyTotalTasks = reader["total"] == DBNull.Value ? 0 : Convert.ToInt32(reader["total"]);
                dashboard.MyCompletedTasks = reader["completed"] == DBNull.Value ? 0 : Convert.ToInt32(reader["completed"]);
                dashboard.MyInProgressTasks = reader["in_progress"] == DBNull.Value ? 0 : Convert.ToInt32(reader["in_progress"]);
                dashboard.MyOverdueTasks = reader["overdue"] == DBNull.Value ? 0 : Convert.ToInt32(reader["overdue"]);
            }
        }

        // Get hours this week
        try
        {
            using var cmd = new MySqlCommand("sp_GetEmployeeHoursThisWeek", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_user_id", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                dashboard.MyHoursThisWeek = reader["hours_this_week"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["hours_this_week"]);
            }
        }
        catch
        {
            dashboard.MyHoursThisWeek = 0;
        }

        // Get hours this month
        try
        {
            using var cmd = new MySqlCommand("sp_GetEmployeeHoursThisMonth", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_user_id", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                dashboard.MyHoursThisMonth = reader["hours_this_month"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["hours_this_month"]);
            }
        }
        catch
        {
            dashboard.MyHoursThisMonth = 0;
        }

        // Get today's tasks
        using (var cmd = new MySqlCommand("sp_GetEmployeeTodaysTasks", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_user_id", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dashboard.TodaysTasks.Add(new TaskItemVM
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Title = reader["title"]?.ToString() ?? "",
                    DueDate = reader["due_date"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["due_date"]),
                    PriorityName = reader["priority_name"]?.ToString() ?? "Unknown",
                    StatusName = reader["status_name"]?.ToString() ?? "Unknown"
                });
            }
        }

        // Get upcoming tasks
        using (var cmd = new MySqlCommand("sp_GetEmployeeUpcomingTasks", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_user_id", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dashboard.UpcomingTasks.Add(new TaskItemVM
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Title = reader["title"]?.ToString() ?? "",
                    DueDate = reader["due_date"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["due_date"]),
                    PriorityName = reader["priority_name"]?.ToString() ?? "Unknown",
                    StatusName = reader["status_name"]?.ToString() ?? "Unknown"
                });
            }
        }

        // Get overdue tasks
        using (var cmd = new MySqlCommand("sp_GetEmployeeOverdueTasks", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_user_id", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dashboard.OverdueTasks.Add(new TaskItemVM
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Title = reader["title"]?.ToString() ?? "",
                    DueDate = reader["due_date"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["due_date"]),
                    PriorityName = reader["priority_name"]?.ToString() ?? "Unknown",
                    StatusName = reader["status_name"]?.ToString() ?? "Unknown"
                });
            }
        }

        // Get recently completed
        using (var cmd = new MySqlCommand("sp_GetEmployeeRecentlyCompleted", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_user_id", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dashboard.RecentlyCompleted.Add(new TaskItemVM
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Title = reader["title"]?.ToString() ?? "",
                    DueDate = reader["due_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["due_date"]),
                    PriorityName = reader["priority_name"]?.ToString() ?? "Unknown",
                    StatusName = reader["status_name"]?.ToString() ?? "Unknown"
                });
            }
        }

        return dashboard;
    }
}

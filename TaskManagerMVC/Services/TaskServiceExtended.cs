using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;
using System.Data;
using TaskManagerMVC.Data;
using TaskManagerMVC.Models;
using TaskManagerMVC.ViewModels;

namespace TaskManagerMVC.Services;

/// <summary>
/// Extended Task Service for Admin and Employee features
/// </summary>
public partial class TaskService
{
    // ============================================
    // ADMIN TASK MANAGEMENT
    // ============================================

    /// <summary>
    /// Get all tasks for admin with filters
    /// </summary>
    public async Task<TaskListVM> GetTasksForAdminAsync(int? statusId, int? priorityId, int? projectId, int? assignedTo)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetTasksForAdmin", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_status_id", (object?)statusId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_priority_id", (object?)priorityId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_project_id", (object?)projectId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_assigned_to", (object?)assignedTo ?? DBNull.Value);

        var tasks = new List<TaskItemVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tasks.Add(new TaskItemVM
            {
                Id = Convert.ToInt32(reader["id"]),
                Title = reader["title"].ToString()!,
                Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString(),
                CategoryName = reader["category_name"] == DBNull.Value ? null : reader["category_name"].ToString(),
                PriorityName = reader["priority_name"].ToString()!,
                StatusName = reader["status_name"].ToString()!,
                DueDate = reader["due_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["due_date"]),
                AssigneeName = reader["assignee_name"] == DBNull.Value ? null : reader["assignee_name"].ToString(),
                ProjectName = reader["project_name"] == DBNull.Value ? null : reader["project_name"].ToString()
            });
        }

        return new TaskListVM
        {
            Tasks = tasks,
            StatusId = statusId,
            PriorityId = priorityId,
            ProjectId = projectId,
            Categories = new SelectList(await GetCategoriesAsync(), "Id", "Name"),
            Priorities = new SelectList(await GetPrioritiesAsync(), "Id", "Name"),
            Statuses = new SelectList(await GetStatusesAsync(), "Id", "DisplayName")
        };
    }

    /// <summary>
    /// Get tasks for manager - only shows tasks from projects they manage
    /// </summary>
    public async Task<TaskListVM> GetTasksForManagerAsync(int managerId, int? statusId, int? priorityId, int? projectId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetTasksForManager", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_manager_id", managerId);
        cmd.Parameters.AddWithValue("p_status_id", (object?)statusId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_priority_id", (object?)priorityId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_project_id", (object?)projectId ?? DBNull.Value);

        var tasks = new List<TaskItemVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tasks.Add(new TaskItemVM
            {
                Id = Convert.ToInt32(reader["id"]),
                Title = reader["title"].ToString()!,
                Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString(),
                CategoryName = reader["category_name"] == DBNull.Value ? null : reader["category_name"].ToString(),
                PriorityName = reader["priority_name"].ToString()!,
                StatusName = reader["status_name"].ToString()!,
                DueDate = reader["due_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["due_date"]),
                AssigneeName = reader["assignee_name"] == DBNull.Value ? null : reader["assignee_name"].ToString(),
                ProjectName = reader["project_name"] == DBNull.Value ? null : reader["project_name"].ToString()
            });
        }

        return new TaskListVM
        {
            Tasks = tasks,
            StatusId = statusId,
            PriorityId = priorityId,
            ProjectId = projectId,
            Categories = new SelectList(await GetCategoriesAsync(), "Id", "Name"),
            Priorities = new SelectList(await GetPrioritiesAsync(), "Id", "Name"),
            Statuses = new SelectList(await GetStatusesAsync(), "Id", "DisplayName")
        };
    }

    /// <summary>
    /// Get task assignment form for admin
    /// </summary>
    public async Task<TaskAssignmentVM> GetTaskAssignmentFormAsync(int? id = null, int? currentUserId = null)
    {
        var vm = new TaskAssignmentVM
        {
            Projects = new SelectList(await GetProjectsAsync(), "Id", "Name"),
            Categories = new SelectList(await GetCategoriesAsync(), "Id", "Name"),
            Priorities = new SelectList(await GetPrioritiesAsync(), "Id", "Name"),
            Statuses = new SelectList(await GetStatusesAsync(), "Id", "DisplayName"),
            Employees = new SelectList(await GetEmployeesAsync(currentUserId), "Id", "FullName"),
            TaskTypes = GetTaskTypeSelectList()
        };

        if (id.HasValue)
        {
            using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("sp_GetTaskForEdit", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_task_id", id.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                vm.Id = Convert.ToInt32(reader["id"]);
                vm.Title = reader["title"].ToString()!;
                vm.Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString()!;
                vm.ProjectId = reader["project_id"] == DBNull.Value ? null : Convert.ToInt32(reader["project_id"]);
                vm.CategoryId = reader["category_id"] == DBNull.Value ? null : Convert.ToInt32(reader["category_id"]);
                vm.PriorityId = Convert.ToInt32(reader["priority_id"]);
                vm.StatusId = Convert.ToInt32(reader["status_id"]);
                vm.TaskType = reader["task_type"] == DBNull.Value ? "task" : reader["task_type"].ToString()!;
                vm.AssignedTo = reader["assigned_to"] == DBNull.Value ? null : Convert.ToInt32(reader["assigned_to"]);
                vm.StartDate = reader["start_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["start_date"]);
                vm.DueDate = Convert.ToDateTime(reader["due_date"]);
                vm.EstimatedHours = reader["estimated_hours"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["estimated_hours"]);
                vm.IsBillable = Convert.ToBoolean(reader["is_billable"]);
                vm.HourlyRate = reader["hourly_rate"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["hourly_rate"]);
                vm.Tags = reader["tags"] == DBNull.Value ? null : reader["tags"].ToString()!;
            }
        }

        return vm;
    }

    /// <summary>
    /// Assign a new task to an employee
    /// </summary>
    public async Task<int> AssignTaskAsync(int adminId, TaskAssignmentVM model)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_CreateTaskExtended", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_title", model.Title);
        cmd.Parameters.AddWithValue("p_description", (object?)model.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_project_id", (object?)model.ProjectId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_category_id", (object?)model.CategoryId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_priority_id", model.PriorityId);
        cmd.Parameters.AddWithValue("p_status_id", model.StatusId);
        cmd.Parameters.AddWithValue("p_task_type", model.TaskType);
        cmd.Parameters.AddWithValue("p_assigned_to", (object?)model.AssignedTo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_assigned_by", adminId);
        cmd.Parameters.AddWithValue("p_assigned_at", model.AssignedTo.HasValue ? DateTime.Now : DBNull.Value);
        cmd.Parameters.AddWithValue("p_start_date", (object?)model.StartDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_due_date", model.DueDate);
        cmd.Parameters.AddWithValue("p_estimated_hours", model.EstimatedHours);
        cmd.Parameters.AddWithValue("p_is_billable", model.IsBillable);
        cmd.Parameters.AddWithValue("p_hourly_rate", model.HourlyRate);
        cmd.Parameters.AddWithValue("p_tags", (object?)model.Tags ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_created_by", adminId);
        cmd.Parameters.AddWithValue("p_created_at", DateTime.Now);
        cmd.Parameters.AddWithValue("p_updated_at", DateTime.Now);

        using var reader = await cmd.ExecuteReaderAsync();
        int taskId = 0;
        if (await reader.ReadAsync())
        {
            taskId = Convert.ToInt32(reader["id"]);
        }

        // Send notification if task is assigned to someone
        if (model.AssignedTo.HasValue)
        {
            await _notificationService.NotifyTaskAssignedAsync(taskId, model.AssignedTo.Value, adminId, model.Title);
        }

        return taskId;
    }

    /// <summary>
    /// Update task assignment
    /// </summary>
    public async Task UpdateTaskAssignmentAsync(int adminId, TaskAssignmentVM model)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        // Get the old assignee to check if it changed
        using var getCmd = new MySqlCommand("sp_GetOldAssignee", conn);
        getCmd.CommandType = CommandType.StoredProcedure;
        getCmd.Parameters.AddWithValue("p_task_id", model.Id);
        var oldAssigneeObj = await getCmd.ExecuteScalarAsync();
        var oldAssignee = oldAssigneeObj == DBNull.Value ? (int?)null : Convert.ToInt32(oldAssigneeObj);

        // SECURITY: Fetch existing task to ensure StartDate is not modified (prevent backdating/tampering)
        var existingTask = await GetTaskAssignmentFormAsync(model.Id);

        using var cmd = new MySqlCommand("sp_UpdateTaskExtended", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_id", model.Id);  // Changed from p_task_id to p_id
        cmd.Parameters.AddWithValue("p_title", model.Title);
        cmd.Parameters.AddWithValue("p_description", (object?)model.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_project_id", (object?)model.ProjectId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_category_id", (object?)model.CategoryId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_priority_id", model.PriorityId);
        cmd.Parameters.AddWithValue("p_status_id", model.StatusId);
        cmd.Parameters.AddWithValue("p_task_type", model.TaskType);
        cmd.Parameters.AddWithValue("p_assigned_to", (object?)model.AssignedTo ?? DBNull.Value);
        // Force original StartDate
        cmd.Parameters.AddWithValue("p_start_date", (object?)existingTask.StartDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_due_date", model.DueDate);
        cmd.Parameters.AddWithValue("p_estimated_hours", model.EstimatedHours);
        cmd.Parameters.AddWithValue("p_is_billable", model.IsBillable);
        cmd.Parameters.AddWithValue("p_hourly_rate", model.HourlyRate);
        cmd.Parameters.AddWithValue("p_tags", (object?)model.Tags ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_updated_at", DateTime.Now);

        await cmd.ExecuteNonQueryAsync();

        // Send notification if assignee changed
        if (model.AssignedTo.HasValue && model.AssignedTo != oldAssignee)
        {
            await _notificationService.NotifyTaskAssignedAsync(model.Id, model.AssignedTo.Value, adminId, model.Title);
        }
        else if (model.AssignedTo.HasValue)
        {
            // Task was updated but assignee stayed the same
            await _notificationService.NotifyTaskUpdatedAsync(model.Id, model.AssignedTo.Value, model.Title, "Task details have been updated");
        }
    }

    // ============================================
    // EMPLOYEE TASK MANAGEMENT
    // ============================================

    /// <summary>
    /// Get tasks assigned to an employee
    /// </summary>
    public async Task<TaskListVM> GetEmployeeTasksAsync(int userId, int? statusId, int? priorityId, string? sortBy)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetTasksForEmployee", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_status_id", (object?)statusId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_priority_id", (object?)priorityId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_sort_by", (object?)sortBy ?? DBNull.Value);

        var tasks = new List<TaskItemVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tasks.Add(new TaskItemVM
            {
                Id = Convert.ToInt32(reader["id"]),
                Title = reader["title"].ToString()!,
                Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString(),
                CategoryName = reader["category_name"] == DBNull.Value ? null : reader["category_name"].ToString(),
                PriorityName = reader["priority_name"].ToString()!,
                StatusId = Convert.ToInt32(reader["status_id"]),
                StatusName = reader["status_name"].ToString()!,
                DueDate = reader["due_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["due_date"])
            });
        }

        return new TaskListVM
        {
            Tasks = tasks,
            StatusId = statusId,
            PriorityId = priorityId,
            Priorities = new SelectList(await GetPrioritiesAsync(), "Id", "Name"),
            Statuses = new SelectList(await GetStatusesAsync(), "Id", "DisplayName")
        };
    }

    /// <summary>
    /// Get enhanced task details with comments and activity
    /// </summary>
    public async Task<TaskDetailsEnhancedVM?> GetTaskDetailsEnhancedAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT t.id, t.title, t.description, t.due_date, t.start_date, t.completed_date, 
                   t.task_type, t.progress, t.estimated_hours, t.actual_hours, t.is_billable, t.tags,
                   t.created_at, t.updated_at, t.project_id, t.assigned_to, t.assigned_at, t.created_by,
                   c.name as category_name, p.name as priority_name, s.name as status_name,
                   CONCAT(u.first_name, ' ', u.last_name) as assignee_name, u.profile_image as assignee_image,
                   CONCAT(creator.first_name, ' ', creator.last_name) as creator_name,
                   CONCAT(assigner.first_name, ' ', assigner.last_name) as assigned_by_name,
                   proj.name as project_name, proj.code as project_code
            FROM tasks t
            LEFT JOIN categories c ON t.category_id = c.id
            LEFT JOIN priorities p ON t.priority_id = p.id
            LEFT JOIN statuses s ON t.status_id = s.id
            LEFT JOIN users u ON t.assigned_to = u.id
            LEFT JOIN users creator ON t.created_by = creator.id
            LEFT JOIN users assigner ON t.assigned_by = assigner.id
            LEFT JOIN projects proj ON t.project_id = proj.id
            WHERE t.id = @taskId";

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@taskId", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var model = new TaskDetailsEnhancedVM
        {
            Id = Convert.ToInt32(reader["id"]),
            Title = reader["title"].ToString()!,
            Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString()!,
            DueDate = reader["due_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["due_date"]),
            StartDate = reader["start_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["start_date"]),
            CompletedDate = reader["completed_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["completed_date"]),
            TaskType = reader["task_type"] == DBNull.Value ? "task" : reader["task_type"].ToString()!,
            Progress = Convert.ToInt32(reader["progress"]),
            EstimatedHours = reader["estimated_hours"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["estimated_hours"]),
            ActualHours = reader["actual_hours"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["actual_hours"]),
            IsBillable = Convert.ToBoolean(reader["is_billable"]),
            Tags = reader["tags"] == DBNull.Value ? null : reader["tags"].ToString()!,
            CreatedAt = Convert.ToDateTime(reader["created_at"]),
            UpdatedAt = reader["updated_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["updated_at"]),
            CategoryName = reader["category_name"] == DBNull.Value ? null : reader["category_name"].ToString()!,
            PriorityName = reader["priority_name"].ToString()!,
            StatusName = reader["status_name"].ToString()!,
            ProjectId = reader["project_id"] == DBNull.Value ? null : Convert.ToInt32(reader["project_id"]),
            ProjectName = reader["project_name"] == DBNull.Value ? null : reader["project_name"].ToString()!,
            ProjectCode = reader["project_code"] == DBNull.Value ? null : reader["project_code"].ToString()!,
            AssignedTo = reader["assigned_to"] == DBNull.Value ? null : Convert.ToInt32(reader["assigned_to"]),
            AssigneeName = reader["assignee_name"] == DBNull.Value ? null : reader["assignee_name"].ToString()!,
            AssigneeImage = reader["assignee_image"] == DBNull.Value ? null : reader["assignee_image"].ToString()!,
            AssignedByName = reader["assigned_by_name"] == DBNull.Value ? null : reader["assigned_by_name"].ToString()!,
            AssignedAt = reader["assigned_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["assigned_at"]),
            CreatedById = Convert.ToInt32(reader["created_by"]),
            CreatorName = reader["creator_name"] == DBNull.Value ? null : reader["creator_name"].ToString()!
        };

        // Close the reader before executing another query
        await reader.CloseAsync();

        // Load comments for this task
        using var commentsCmd = new MySqlCommand("sp_GetTaskComments", conn);
        commentsCmd.CommandType = CommandType.StoredProcedure;
        commentsCmd.Parameters.AddWithValue("p_task_id", id);
        
        using var commentsReader = await commentsCmd.ExecuteReaderAsync();
        while (await commentsReader.ReadAsync())
        {
            model.Comments.Add(new TaskCommentVM
            {
                Id = Convert.ToInt32(commentsReader["id"]),
                TaskId = Convert.ToInt32(commentsReader["task_id"]),
                UserId = Convert.ToInt32(commentsReader["user_id"]),
                UserName = commentsReader["user_name"] == DBNull.Value ? "Unknown" : commentsReader["user_name"].ToString()!,
                UserImage = commentsReader["user_image"] == DBNull.Value ? null : commentsReader["user_image"].ToString(),
                Comment = commentsReader["comment"].ToString()!,
                CommentType = commentsReader["comment_type"] == DBNull.Value ? "comment" : commentsReader["comment_type"].ToString()!,
                IsInternal = Convert.ToBoolean(commentsReader["is_internal"]),
                CreatedAt = Convert.ToDateTime(commentsReader["created_at"])
            });
        }
        await commentsReader.CloseAsync();

        // Load attachments
        using var attachCmd = new MySqlCommand("sp_GetAttachments", conn);
        attachCmd.CommandType = CommandType.StoredProcedure;
        attachCmd.Parameters.AddWithValue("p_task_id", id);

        using var attachReader = await attachCmd.ExecuteReaderAsync();
        while (await attachReader.ReadAsync())
        {
            long sizeBytes = Convert.ToInt64(attachReader["file_size"]);
            string sizeStr = sizeBytes < 1024 * 1024 
                ? $"{(sizeBytes / 1024.0):F1} KB" 
                : $"{(sizeBytes / (1024.0 * 1024.0)):F1} MB";

            model.Attachments.Add(new AttachmentVM
            {
                Id = Convert.ToInt32(attachReader["id"]),
                FileName = attachReader["file_name"].ToString()!,
                OriginalName = attachReader["original_name"].ToString()!,
                FileType = attachReader["file_type"] == DBNull.Value ? null : attachReader["file_type"].ToString(),
                FileSize = sizeStr,
                FilePath = $"/File/Attachment/{Convert.ToInt32(attachReader["id"])}",
                UploaderName = attachReader["uploaded_by_name"] == DBNull.Value ? "Unknown" : attachReader["uploaded_by_name"].ToString()!,
                CreatedAt = Convert.ToDateTime(attachReader["created_at"])
            });
        }

        return model;
    }

    /// <summary>
    /// Update task status
    /// </summary>
    public async Task UpdateTaskStatusAsync(int taskId, int statusId, int userId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        var completedDate = statusId == 7 ? DateTime.Now : (DateTime?)null; // 7 = completed

        using var cmd = new MySqlCommand("sp_UpdateTaskStatus", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_task_id", taskId);
        cmd.Parameters.AddWithValue("p_status_id", statusId);
        cmd.Parameters.AddWithValue("p_completed_date", (object?)completedDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_updated_at", DateTime.Now);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Update task progress
    /// </summary>
    public async Task UpdateTaskProgressAsync(int taskId, int progress, int userId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_UpdateTaskProgress", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_task_id", taskId);
        cmd.Parameters.AddWithValue("p_progress", Math.Min(100, Math.Max(0, progress)));
        cmd.Parameters.AddWithValue("p_updated_at", DateTime.Now);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Get user time logs
    /// </summary>
    public async Task<List<TimeLogVM>> GetUserTimeLogsAsync(int userId, DateTime startDate, DateTime endDate)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetTimeLogs", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_start_date", startDate);
        cmd.Parameters.AddWithValue("p_end_date", endDate);

        var logs = new List<TimeLogVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            logs.Add(new TimeLogVM
            {
                Id = Convert.ToInt32(reader["id"]),
                TaskId = Convert.ToInt32(reader["task_id"]),
                UserId = userId,
                HoursLogged = Convert.ToDecimal(reader["hours_logged"]),
                LogDate = Convert.ToDateTime(reader["log_date"]),
                Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString()!,
                IsBillable = Convert.ToBoolean(reader["is_billable"]),
                IsApproved = Convert.ToBoolean(reader["is_approved"])
            });
            Console.WriteLine($"[DEBUG] GetUserTimeLogsAsync: Id={reader["id"]}, Hours={reader["hours_logged"]}, IsBillable={reader["is_billable"]}");
        }

        return logs;
    }

    /// <summary>
    /// Get status select list
    /// </summary>
    public async Task<SelectList> GetStatusSelectListAsync()
    {
        return new SelectList(await GetStatusesAsync(), "Id", "DisplayName");
    }

    // ============================================
    // HELPER METHODS
    // ============================================

    private async Task<List<Project>> GetProjectsAsync()
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

    private async Task<List<User>> GetEmployeesAsync(int? excludeUserId = null)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        
        using var cmd = new MySqlCommand("sp_GetActiveUsers", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_exclude_user_id", (object?)excludeUserId ?? DBNull.Value);
        
        var users = new List<User>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            users.Add(new User { Id = Convert.ToInt32(reader["id"]), FirstName = reader["full_name"].ToString()!, LastName = "" });
        return users;
    }

    private SelectList GetTaskTypeSelectList()
    {
        var types = new List<object>
        {
            new { Value = "task", Text = "Task" },
            new { Value = "bug", Text = "Bug" },
            new { Value = "feature", Text = "Feature" },
            new { Value = "improvement", Text = "Improvement" },
            new { Value = "support", Text = "Support" }
        };
        return new SelectList(types, "Value", "Text");
    }
}

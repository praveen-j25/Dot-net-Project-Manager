using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;
using System.Data;
using TaskManagerMVC.Data;
using TaskManagerMVC.Models;
using TaskManagerMVC.ViewModels;

namespace TaskManagerMVC.Services;

public partial class TaskService
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly NotificationService _notificationService;

    public TaskService(DbConnectionFactory dbFactory, NotificationService notificationService)
    {
        _dbFactory = dbFactory;
        _notificationService = notificationService;
    }

    public async Task<TaskListVM> GetTasksAsync(int userId, int? statusId, int? priorityId, int? categoryId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetTasksForUser", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_status_id", (object?)statusId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_priority_id", (object?)priorityId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_category_id", (object?)categoryId ?? DBNull.Value);

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
                DueDate = Convert.ToDateTime(reader["due_date"])
            });
        }

        return new TaskListVM
        {
            Tasks = tasks,
            StatusId = statusId,
            PriorityId = priorityId,
            CategoryId = categoryId,
            Categories = new SelectList(await GetCategoriesAsync(), "Id", "Name"),
            Priorities = new SelectList(await GetPrioritiesAsync(), "Id", "Name"),
            Statuses = new SelectList(await GetStatusesAsync(), "Id", "Name")
        };
    }

    public async Task<TaskDetailsVM?> GetTaskByIdAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetTaskById", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_task_id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new TaskDetailsVM
        {
            Id = Convert.ToInt32(reader["id"]),
            Title = reader["title"].ToString()!,
            Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString(),
            CategoryName = reader["category_name"] == DBNull.Value ? null : reader["category_name"].ToString(),
            PriorityName = reader["priority_name"].ToString()!,
            StatusName = reader["status_name"].ToString()!,
            DueDate = reader["due_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["due_date"]),
            CreatedAt = Convert.ToDateTime(reader["created_at"]),
            UpdatedAt = reader["updated_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["updated_at"]),
            CreatedById = reader["created_by"] == DBNull.Value ? 0 : Convert.ToInt32(reader["created_by"]),
            CreatedByName = reader["creator_first"] == DBNull.Value ? null : 
                $"{reader["creator_first"]} {reader["creator_last"]}",
            HourlyRate = reader["hourly_rate"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["hourly_rate"]),
            AssignedToId = reader["assigned_to"] == DBNull.Value ? null : Convert.ToInt32(reader["assigned_to"]),
            ProjectId = reader["project_id"] == DBNull.Value ? null : Convert.ToInt32(reader["project_id"]),
            ProjectManagerId = reader["project_manager_id"] == DBNull.Value ? null : Convert.ToInt32(reader["project_manager_id"])
        };
    }

    public async Task<TaskFormVM> GetTaskFormAsync(int? id = null)
    {
        var vm = new TaskFormVM
        {
            Categories = new SelectList(await GetCategoriesAsync(), "Id", "Name"),
            Priorities = new SelectList(await GetPrioritiesAsync(), "Id", "Name"),
            Statuses = new SelectList(await GetStatusesAsync(), "Id", "Name")
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
                vm.Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString();
                vm.CategoryId = reader["category_id"] == DBNull.Value ? null : Convert.ToInt32(reader["category_id"]);
                vm.PriorityId = Convert.ToInt32(reader["priority_id"]);
                vm.StatusId = Convert.ToInt32(reader["status_id"]);
                vm.DueDate = Convert.ToDateTime(reader["due_date"]);
            }
        }

        return vm;
    }

    public async Task<int> CreateTaskAsync(TaskFormVM model, int createdByUserId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_CreateTask", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_title", model.Title);
        cmd.Parameters.AddWithValue("p_description", (object?)model.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_project_id", DBNull.Value);
        cmd.Parameters.AddWithValue("p_category_id", (object?)model.CategoryId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_priority_id", model.PriorityId);
        cmd.Parameters.AddWithValue("p_status_id", model.StatusId);
        cmd.Parameters.AddWithValue("p_due_date", model.DueDate);
        cmd.Parameters.AddWithValue("p_estimated_hours", 0);
        cmd.Parameters.AddWithValue("p_hourly_rate", model.HourlyRate);
        cmd.Parameters.AddWithValue("p_created_by", createdByUserId);
        cmd.Parameters.AddWithValue("p_created_at", DateTime.Now);
        cmd.Parameters.AddWithValue("p_updated_at", DateTime.Now);

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var taskId = Convert.ToInt32(reader["id"]);

        return taskId;
    }

    public async Task UpdateTaskAsync(TaskFormVM model, int userId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_UpdateTask", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_id", model.Id);
        cmd.Parameters.AddWithValue("p_title", model.Title);
        cmd.Parameters.AddWithValue("p_description", (object?)model.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_category_id", (object?)model.CategoryId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_priority_id", model.PriorityId);
        cmd.Parameters.AddWithValue("p_status_id", model.StatusId);
        cmd.Parameters.AddWithValue("p_due_date", model.DueDate);
        cmd.Parameters.AddWithValue("p_estimated_hours", 0);
        cmd.Parameters.AddWithValue("p_hourly_rate", model.HourlyRate);
        cmd.Parameters.AddWithValue("p_updated_at", DateTime.Now);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteTaskAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_DeleteTask", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_task_id", id);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<DashboardVM> GetDashboardAsync(int userId, string userName)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        // Get task counts by status
        using var countCmd = new MySqlCommand("sp_GetTaskCountsByStatus", conn);
        countCmd.CommandType = CommandType.StoredProcedure;

        var statusCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int totalTasks = 0;

        using (var reader = await countCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var statusName = reader["status_name"] == DBNull.Value ? "Unknown" : reader["status_name"].ToString()!;
                var count = Convert.ToInt32(reader["cnt"]);
                statusCounts[statusName] = count;
                totalTasks += count;
            }
        }

        // Get overdue tasks
        using var overdueCmd = new MySqlCommand("sp_GetOverdueTasks", conn);
        overdueCmd.CommandType = CommandType.StoredProcedure;

        var overdueTasks = new List<TaskItemVM>();
        using (var reader = await overdueCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                overdueTasks.Add(new TaskItemVM
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Title = reader["title"].ToString()!,
                    DueDate = Convert.ToDateTime(reader["due_date"]),
                    CategoryName = reader["category_name"] == DBNull.Value ? "Uncategorized" : reader["category_name"].ToString()
                });
            }
        }

        // Get recent tasks
        using var recentCmd = new MySqlCommand("sp_GetRecentTasksList", conn);
        recentCmd.CommandType = CommandType.StoredProcedure;

        var recentTasks = new List<TaskItemVM>();
        using (var reader = await recentCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                recentTasks.Add(new TaskItemVM
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Title = reader["title"].ToString()!,
                    StatusName = reader["status_name"].ToString()!,
                    CategoryName = reader["category_name"] == DBNull.Value ? "Uncategorized" : reader["category_name"].ToString()
                });
            }
        }

        return new DashboardVM
        {
            UserName = userName,
            TotalTasks = totalTasks,
            PendingTasks = statusCounts.GetValueOrDefault("pending", 0),
            InProgressTasks = statusCounts.GetValueOrDefault("in_progress", 0) + statusCounts.GetValueOrDefault("in_review", 0) + statusCounts.GetValueOrDefault("testing", 0),
            CompletedTasks = statusCounts.GetValueOrDefault("completed", 0),
            OverdueTasks = overdueTasks,
            RecentTasks = recentTasks
        };
    }

    // Helper methods for lookups
    private async Task<List<Category>> GetCategoriesAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetCategories", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        var categories = new List<Category>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            categories.Add(new Category
            {
                Id = Convert.ToInt32(reader["id"]),
                Name = reader["name"].ToString()!,
                Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString(),
                Color = reader["color"].ToString()!,
                Icon = reader["icon"].ToString()!,
                IsActive = Convert.ToBoolean(reader["is_active"])
            });
        }
        return categories;
    }

    private async Task<List<Priority>> GetPrioritiesAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetPriorities", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        var priorities = new List<Priority>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            priorities.Add(new Priority
            {
                Id = Convert.ToInt32(reader["id"]),
                Name = reader["name"].ToString()!,
                Level = Convert.ToInt32(reader["level"]),
                Color = reader["color"].ToString()!
            });
        }
        return priorities;
    }

    private async Task<List<Status>> GetStatusesAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetStatuses", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        var statuses = new List<Status>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            statuses.Add(new Status
            {
                Id = Convert.ToInt32(reader["id"]),
                Name = reader["name"].ToString()!,
                DisplayName = reader["display_name"].ToString()!,
                Color = reader["color"].ToString()!,
                SortOrder = Convert.ToInt32(reader["sort_order"])
            });
        }
        return statuses;
    }

    // =============== FILE ATTACHMENT METHODS ===============

    public async Task SaveAttachmentsAsync(int taskId, int userId, List<IFormFile> files)
    {
        Console.WriteLine($"[DEBUG] SaveAttachmentsAsync called. TaskId: {taskId}, Files: {files?.Count ?? 0}");
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        foreach (var file in files)
        {
            if (file.Length == 0 || file.Length > 10 * 1024 * 1024) continue; // Skip empty or >10MB

            var ext = Path.GetExtension(file.FileName).TrimStart('.');
            var safeFileName = $"{Guid.NewGuid():N}.{ext}";

            // Read file into byte array
            byte[] fileContent;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileContent = ms.ToArray();
            }

            using var cmd = new MySqlCommand("sp_SaveAttachment", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_task_id", taskId);
            cmd.Parameters.AddWithValue("p_uploaded_by", userId);
            cmd.Parameters.AddWithValue("p_file_name", safeFileName);
            cmd.Parameters.AddWithValue("p_original_name", file.FileName);
            cmd.Parameters.AddWithValue("p_file_type", ext);
            cmd.Parameters.AddWithValue("p_file_size", file.Length);
            cmd.Parameters.Add("p_file_content", MySqlDbType.LongBlob).Value = fileContent;
            cmd.Parameters.AddWithValue("p_content_type", file.ContentType ?? "application/octet-stream");
            cmd.Parameters.AddWithValue("p_created_at", DateTime.Now);

            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task<List<TaskAttachmentVM>> GetAttachmentsAsync(int taskId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetAttachments", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_task_id", taskId);

        var attachments = new List<TaskAttachmentVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            attachments.Add(new TaskAttachmentVM
            {
                Id = Convert.ToInt32(reader["id"]),
                FileName = reader["file_name"].ToString()!,
                OriginalName = reader["original_name"].ToString()!,
                FileType = reader["file_type"] == DBNull.Value ? null : reader["file_type"].ToString(),
                FileSize = Convert.ToInt64(reader["file_size"]),
                CreatedAt = Convert.ToDateTime(reader["created_at"]),
                UploadedByName = reader["uploaded_by_name"] == DBNull.Value ? null : reader["uploaded_by_name"].ToString()
            });
        }
        return attachments;
    }

    public async Task<(byte[]? Content, string ContentType, string FileName)?> DownloadAttachmentAsync(int attachmentId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_DownloadAttachment", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_id", attachmentId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var content = reader["file_content"] == DBNull.Value ? null : (byte[])reader["file_content"];
        var contentType = reader["content_type"]?.ToString() ?? "application/octet-stream";
        var fileName = reader["original_name"]?.ToString() ?? reader["file_name"]?.ToString() ?? "download";

        return (content, contentType, fileName);
    }

    public async Task DeleteAttachmentAsync(int attachmentId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_DeleteAttachment", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_id", attachmentId);
        await cmd.ExecuteNonQueryAsync();
    }
}

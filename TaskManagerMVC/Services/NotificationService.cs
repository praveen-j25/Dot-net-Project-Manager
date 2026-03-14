using MySql.Data.MySqlClient;
using System.Data;
using TaskManagerMVC.Data;
using TaskManagerMVC.Models;
using TaskManagerMVC.ViewModels;

namespace TaskManagerMVC.Services;

public class NotificationService
{
    private readonly DbConnectionFactory _dbFactory;

    public NotificationService(DbConnectionFactory dbFactory) => _dbFactory = dbFactory;

    /// <summary>
    /// Get user notifications
    /// </summary>
    public async Task<List<NotificationVM>> GetUserNotificationsAsync(int userId, int limit = 20, bool unreadOnly = false)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetUserNotifications", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_limit", limit);
        cmd.Parameters.AddWithValue("p_unread_only", unreadOnly);

        var notifications = new List<NotificationVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            notifications.Add(new NotificationVM
            {
                Id = Convert.ToInt32(reader["id"]),
                Title = reader["title"].ToString()!,
                Message = reader["message"].ToString()!,
                Type = reader["type"].ToString()!,
                ReferenceType = reader["reference_type"] == DBNull.Value ? null : reader["reference_type"].ToString()!,
                ReferenceId = reader["reference_id"] == DBNull.Value ? null : Convert.ToInt32(reader["reference_id"]),
                IsRead = Convert.ToBoolean(reader["is_read"]),
                CreatedAt = Convert.ToDateTime(reader["created_at"])
            });
        }

        return notifications;
    }


    /// <summary>
    /// Get unread notification count
    /// </summary>
    public async Task<int> GetUnreadCountAsync(int userId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetUnreadNotificationCount", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", userId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Convert.ToInt32(reader["count"]);
        }
        return 0;
    }


    /// <summary>
    /// Mark notification as read
    /// </summary>
    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_MarkNotificationAsRead", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_id", notificationId);
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_read_at", DateTime.Now);

        await cmd.ExecuteNonQueryAsync();
    }


    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    public async Task MarkAllAsReadAsync(int userId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_MarkAllNotificationsAsRead", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_read_at", DateTime.Now);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Create task assignment notification
    /// </summary>
    public async Task NotifyTaskAssignedAsync(int taskId, int assigneeId, int assignerId, string taskTitle)
    {
        await CreateNotificationAsync(new Notification
        {
            UserId = assigneeId,
            Title = "New Task Assigned",
            Message = $"You have been assigned a new task: \"{taskTitle}\"",
            Type = "task_assigned",
            ReferenceType = "task",
            ReferenceId = taskId
        });
    }

    /// <summary>
    /// Create task update notification
    /// </summary>
    public async Task NotifyTaskUpdatedAsync(int taskId, int userId, string taskTitle, string updateDescription)
    {
        await CreateNotificationAsync(new Notification
        {
            UserId = userId,
            Title = "Task Updated",
            Message = $"Task \"{taskTitle}\" has been updated: {updateDescription}",
            Type = "task_updated",
            ReferenceType = "task",
            ReferenceId = taskId
        });
    }

    /// <summary>
    /// Create comment notification
    /// </summary>
    public async Task NotifyCommentAddedAsync(int taskId, int userId, string taskTitle, string commenterName)
    {
        await CreateNotificationAsync(new Notification
        {
            UserId = userId,
            Title = "New Comment",
            Message = $"{commenterName} commented on task \"{taskTitle}\"",
            Type = "comment_added",
            ReferenceType = "task",
            ReferenceId = taskId
        });
    }

    /// <summary>
    /// Create deadline reminder notification
    /// </summary>
    public async Task NotifyDeadlineReminderAsync(int taskId, int userId, string taskTitle, int daysRemaining)
    {
        var urgencyText = daysRemaining <= 0 ? "is overdue" : daysRemaining == 1 ? "is due tomorrow" : $"is due in {daysRemaining} days";
        
        await CreateNotificationAsync(new Notification
        {
            UserId = userId,
            Title = "Deadline Reminder",
            Message = $"Task \"{taskTitle}\" {urgencyText}",
            Type = "deadline_reminder",
            ReferenceType = "task",
            ReferenceId = taskId
        });
    }

    /// <summary>
    /// Notify all admins about a new event
    /// </summary>
    public async Task NotifyAdminsAsync(string title, string message, string type, string? referenceType = null, int? referenceId = null)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        // Get all admin IDs (Role ID = 1)
        using var cmd = new MySqlCommand("sp_GetAdminUserIds", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        
        var adminIds = new List<int>();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                adminIds.Add(Convert.ToInt32(reader["id"]));
            }
        }

        // Create notification for each admin
        foreach (var adminId in adminIds)
        {
            await CreateNotificationAsync(new Notification
            {
                UserId = adminId,
                Title = title,
                Message = message,
                Type = type,
                ReferenceType = referenceType,
                ReferenceId = referenceId
            });
        }
    }

    /// <summary>
    /// Create a notification
    /// </summary>
    private async Task CreateNotificationAsync(Notification notification)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_CreateNotification", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", notification.UserId);
        cmd.Parameters.AddWithValue("p_title", notification.Title);
        cmd.Parameters.AddWithValue("p_message", notification.Message);
        cmd.Parameters.AddWithValue("p_type", notification.Type);
        cmd.Parameters.AddWithValue("p_reference_type", (object?)notification.ReferenceType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_reference_id", (object?)notification.ReferenceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_created_at", DateTime.Now);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Delete old notifications (cleanup)
    /// </summary>
    public async Task CleanupOldNotificationsAsync(int daysOld = 30)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_CleanupOldNotifications", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_days", daysOld);

        await cmd.ExecuteNonQueryAsync();
    }
}

public class CommentService
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly NotificationService _notificationService;

    public CommentService(DbConnectionFactory dbFactory, NotificationService notificationService)
    {
        _dbFactory = dbFactory;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Get comments for a task
    /// </summary>
    public async Task<List<TaskCommentVM>> GetTaskCommentsAsync(int taskId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetTaskComments", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_task_id", taskId);

        var comments = new List<TaskCommentVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            comments.Add(new TaskCommentVM
            {
                Id = Convert.ToInt32(reader["id"]),
                TaskId = Convert.ToInt32(reader["task_id"]),
                UserId = Convert.ToInt32(reader["user_id"]),
                UserName = reader["user_name"].ToString()!,
                UserImage = reader["profile_image"] == DBNull.Value ? null : reader["profile_image"].ToString()!,
                Comment = reader["comment"].ToString()!,
                CommentType = reader["comment_type"].ToString()!,
                IsInternal = Convert.ToBoolean(reader["is_internal"]),
                CreatedAt = Convert.ToDateTime(reader["created_at"])
            });
        }

        // Get replies for each comment
        foreach (var comment in comments)
        {
            comment.Replies = await GetCommentRepliesAsync(comment.Id);
        }

        return comments;
    }

    /// <summary>
    /// Get replies for a comment
    /// </summary>
    private async Task<List<TaskCommentVM>> GetCommentRepliesAsync(int parentCommentId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetCommentReplies", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_parent_id", parentCommentId);

        var replies = new List<TaskCommentVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            replies.Add(new TaskCommentVM
            {
                Id = Convert.ToInt32(reader["id"]),
                TaskId = Convert.ToInt32(reader["task_id"]),
                UserId = Convert.ToInt32(reader["user_id"]),
                UserName = reader["user_name"].ToString()!,
                UserImage = reader["profile_image"] == DBNull.Value ? null : reader["profile_image"].ToString()!,
                Comment = reader["comment"].ToString()!,
                CommentType = reader["comment_type"].ToString()!,
                IsInternal = Convert.ToBoolean(reader["is_internal"]),
                CreatedAt = Convert.ToDateTime(reader["created_at"])
            });
        }

        return replies;
    }

    /// <summary>
    /// Add a comment to a task
    /// </summary>
    public async Task<int> AddCommentAsync(int taskId, int userId, string comment, string commentType = "comment", bool isInternal = false, int? parentCommentId = null)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_AddComment", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_task_id", taskId);
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_comment", comment);
        cmd.Parameters.AddWithValue("p_comment_type", commentType);
        cmd.Parameters.AddWithValue("p_is_internal", isInternal);
        cmd.Parameters.AddWithValue("p_parent_comment_id", (object?)parentCommentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_created_at", DateTime.Now);

        using var reader = await cmd.ExecuteReaderAsync();
        int commentId = 0;
        if (await reader.ReadAsync())
        {
            commentId = Convert.ToInt32(reader["id"]);
        }
        reader.Close();

        // Update comment count on task
        using var updateCmd = new MySqlCommand("sp_UpdateTaskCommentCount", conn);
        updateCmd.CommandType = CommandType.StoredProcedure;
        updateCmd.Parameters.AddWithValue("p_task_id", taskId);
        await updateCmd.ExecuteNonQueryAsync();

        return commentId;
    }

    /// <summary>
    /// Add task response (status update with comment)
    /// </summary>
    public async Task AddTaskResponseAsync(int taskId, int userId, TaskResponseVM response)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        // Add comment
        await AddCommentAsync(taskId, userId, response.Comment, 
            response.StatusId.HasValue ? "status_update" : "comment", 
            response.IsInternal);

        // Update task status if changed
        if (response.StatusId.HasValue)
        {
            using var statusCmd = new MySqlCommand("sp_UpdateTaskStatus", conn);
            statusCmd.CommandType = CommandType.StoredProcedure;
            statusCmd.Parameters.AddWithValue("p_task_id", taskId);
            statusCmd.Parameters.AddWithValue("p_status_id", response.StatusId.Value);
            statusCmd.Parameters.AddWithValue("p_updated_at", DateTime.Now);
            await statusCmd.ExecuteNonQueryAsync();
        }

        if (response.Progress.HasValue)
        {
            using var progressCmd = new MySqlCommand("sp_UpdateTaskProgress", conn);
            progressCmd.CommandType = CommandType.StoredProcedure;
            progressCmd.Parameters.AddWithValue("p_task_id", taskId);
            progressCmd.Parameters.AddWithValue("p_progress", response.Progress.Value);
            progressCmd.Parameters.AddWithValue("p_updated_at", DateTime.Now);
            await progressCmd.ExecuteNonQueryAsync();
        }

        // Log time if provided
        if (response.HoursWorked.HasValue && response.HoursWorked.Value > 0)
        {
            await LogTimeAsync(taskId, userId, response.HoursWorked.Value, response.Comment);
        }
    }

    /// <summary>
    /// Log time for a task
    /// </summary>
    public async Task<int> LogTimeAsync(int taskId, int userId, decimal hours, string? description = null, bool isBillable = false)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        // Get project ID from task
        using var taskCmd = new MySqlCommand("sp_GetTaskProjectId", conn);
        taskCmd.CommandType = CommandType.StoredProcedure;
        taskCmd.Parameters.AddWithValue("p_task_id", taskId);
        var projectId = await taskCmd.ExecuteScalarAsync();

        Console.WriteLine($"[DEBUG] Service LogTimeAsync: IsBillable = {isBillable}");

        using var cmd = new MySqlCommand("sp_LogTime", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_task_id", taskId);
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_project_id", projectId == DBNull.Value ? DBNull.Value : projectId);
        cmd.Parameters.AddWithValue("p_hours", hours);
        cmd.Parameters.AddWithValue("p_log_date", DateTime.Today);
        cmd.Parameters.AddWithValue("p_description", (object?)description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_is_billable", isBillable);
        cmd.Parameters.AddWithValue("p_created_at", DateTime.Now);

        using var reader = await cmd.ExecuteReaderAsync();
        int timeLogId = 0;
        if (await reader.ReadAsync())
        {
            timeLogId = Convert.ToInt32(reader["id"]);
        }
        reader.Close();

        // Update actual hours on task
        using var updateCmd = new MySqlCommand("sp_UpdateTaskActualHours", conn);
        updateCmd.CommandType = CommandType.StoredProcedure;
        updateCmd.Parameters.AddWithValue("p_task_id", taskId);
        updateCmd.Parameters.AddWithValue("p_hours", hours);
        updateCmd.Parameters.AddWithValue("p_updated_at", DateTime.Now);
        await updateCmd.ExecuteNonQueryAsync();

        return timeLogId;
    }

    /// <summary>
    /// Get time logs for a task
    /// </summary>
    public async Task<List<TimeLogVM>> GetTaskTimeLogsAsync(int taskId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetTaskTimeLogs", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_task_id", taskId);

        var logs = new List<TimeLogVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            logs.Add(new TimeLogVM
            {
                Id = Convert.ToInt32(reader["id"]),
                TaskId = Convert.ToInt32(reader["task_id"]),
                UserId = Convert.ToInt32(reader["user_id"]),
                UserName = reader["user_name"].ToString()!,
                HoursLogged = Convert.ToDecimal(reader["hours_logged"]),
                LogDate = Convert.ToDateTime(reader["log_date"]),
                Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString()!,
                IsBillable = Convert.ToBoolean(reader["is_billable"]),
                IsApproved = Convert.ToBoolean(reader["is_approved"]),
                ApproverName = reader["approver_name"] == DBNull.Value ? null : reader["approver_name"].ToString()!
            });
        }

        return logs;
    }
}

public class ActivityLogService
{
    private readonly DbConnectionFactory _dbFactory;

    public ActivityLogService(DbConnectionFactory dbFactory) => _dbFactory = dbFactory;

    /// <summary>
    /// Log task activity
    /// </summary>
    public async Task LogActivityAsync(int taskId, int userId, string action, string? fieldChanged = null, string? oldValue = null, string? newValue = null, string? ipAddress = null)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_LogTaskActivity", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_task_id", taskId);
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_action", action);
        cmd.Parameters.AddWithValue("p_field_changed", (object?)fieldChanged ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_old_value", (object?)oldValue ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_new_value", (object?)newValue ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_ip_address", (object?)ipAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_created_at", DateTime.Now);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Get task activity log
    /// </summary>
    public async Task<List<ActivityLogVM>> GetTaskActivityLogAsync(int taskId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetTaskActivityLog", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_task_id", taskId);

        var logs = new List<ActivityLogVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            logs.Add(new ActivityLogVM
            {
                Id = Convert.ToInt32(reader["id"]),
                TaskId = Convert.ToInt32(reader["task_id"]),
                UserName = reader["user_name"].ToString()!,
                UserImage = reader["profile_image"] == DBNull.Value ? null : reader["profile_image"].ToString()!,
                Action = reader["action"].ToString()!,
                FieldChanged = reader["field_changed"] == DBNull.Value ? null : reader["field_changed"].ToString()!,
                OldValue = reader["old_value"] == DBNull.Value ? null : reader["old_value"].ToString()!,
                NewValue = reader["new_value"] == DBNull.Value ? null : reader["new_value"].ToString()!,
                CreatedAt = Convert.ToDateTime(reader["created_at"])
            });
        }

        return logs;
    }

    /// <summary>
    /// Get recent activity for user dashboard
    /// </summary>
    public async Task<List<ActivityLogVM>> GetRecentActivityAsync(int userId, int limit = 10)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetRecentActivity", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_limit", limit);

        var logs = new List<ActivityLogVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            logs.Add(new ActivityLogVM
            {
                Id = Convert.ToInt32(reader["id"]),
                TaskId = Convert.ToInt32(reader["task_id"]),
                UserName = reader["user_name"].ToString()!,
                UserImage = reader["profile_image"] == DBNull.Value ? null : reader["profile_image"].ToString()!,
                Action = reader["action"].ToString()!,
                FieldChanged = reader["field_changed"] == DBNull.Value ? null : reader["field_changed"].ToString()!,
                OldValue = reader["old_value"] == DBNull.Value ? null : reader["old_value"].ToString()!,
                NewValue = reader["new_value"] == DBNull.Value ? null : reader["new_value"].ToString()!,
                CreatedAt = Convert.ToDateTime(reader["created_at"])
            });
        }

        return logs;
    }
}

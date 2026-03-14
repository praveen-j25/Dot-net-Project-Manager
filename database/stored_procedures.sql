-- ============================================
-- COMPLETE STORED PROCEDURES FOR ALL SERVICES
-- Task Manager MVC Application
-- ============================================

USE task_manager_db;

DELIMITER $

-- ============================================
-- AUTHENTICATION SERVICE PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_Login;$
CREATE PROCEDURE sp_Login(
    IN p_email VARCHAR(255)
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.email, u.password, u.phone, u.is_active, u.is_verified, 
           u.created_at, u.last_login, u.role_id, u.job_title, u.employee_id,
           r.name as role_name
    FROM users u
    LEFT JOIN roles r ON u.role_id = r.id
    WHERE u.email = p_email AND u.is_active = 1;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateLastLogin;$
CREATE PROCEDURE sp_UpdateLastLogin(
    IN p_user_id INT,
    IN p_last_login DATETIME
)
BEGIN
    UPDATE users SET last_login = p_last_login WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_CheckEmailExists;$
CREATE PROCEDURE sp_CheckEmailExists(
    IN p_email VARCHAR(255)
)
BEGIN
    SELECT COUNT(*) as count FROM users WHERE email = p_email;
END$$

DROP PROCEDURE IF EXISTS sp_CheckPendingEmailExists;$
CREATE PROCEDURE sp_CheckPendingEmailExists(
    IN p_email VARCHAR(255)
)
BEGIN
    SELECT COUNT(*) as count FROM pending_users WHERE email = p_email AND status = 'pending';
END$$

DROP PROCEDURE IF EXISTS sp_RegisterPendingUser;$
CREATE PROCEDURE sp_RegisterPendingUser(
    IN p_first_name VARCHAR(100),
    IN p_last_name VARCHAR(100),
    IN p_email VARCHAR(255),
    IN p_password VARCHAR(255),
    IN p_phone VARCHAR(20),
    IN p_department_id INT,
    IN p_job_title VARCHAR(100),
    IN p_requested_at DATETIME
)
BEGIN
    INSERT INTO pending_users (first_name, last_name, email, password, phone, department_id, job_title, status, requested_at) 
    VALUES (p_first_name, p_last_name, p_email, p_password, p_phone, p_department_id, p_job_title, 'pending', p_requested_at);
END$$

DROP PROCEDURE IF EXISTS sp_GetUserById;$
CREATE PROCEDURE sp_GetUserById(
    IN p_user_id INT
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.email, u.password, u.phone, u.is_active, u.is_verified, 
           u.created_at, u.last_login, u.role_id, u.department_id, u.team_id, u.job_title, u.employee_id,
           r.name as role_name
    FROM users u
    LEFT JOIN roles r ON u.role_id = r.id
    WHERE u.id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetDepartments;$
CREATE PROCEDURE sp_GetDepartments()
BEGIN
    SELECT id, name FROM departments WHERE is_active = 1 ORDER BY name;
END$$

DROP PROCEDURE IF EXISTS sp_CreatePasswordResetToken;$
CREATE PROCEDURE sp_CreatePasswordResetToken(
    IN p_email VARCHAR(255)
)
BEGIN
    SELECT id FROM users WHERE email = p_email;
END$$

DROP PROCEDURE IF EXISTS sp_InsertPasswordResetToken;$
CREATE PROCEDURE sp_InsertPasswordResetToken(
    IN p_user_id INT,
    IN p_token VARCHAR(255),
    IN p_expiry DATETIME
)
BEGIN
    INSERT INTO password_resets (user_id, reset_token, token_expiry, is_used) 
    VALUES (p_user_id, p_token, p_expiry, 0);
END$$

DROP PROCEDURE IF EXISTS sp_GetPasswordResetToken;$
CREATE PROCEDURE sp_GetPasswordResetToken(
    IN p_token VARCHAR(255),
    IN p_now DATETIME
)
BEGIN
    SELECT pr.id, pr.user_id, u.id as uid 
    FROM password_resets pr 
    INNER JOIN users u ON pr.user_id = u.id 
    WHERE pr.reset_token = p_token AND pr.is_used = 0 AND pr.token_expiry > p_now;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateUserPassword;$
CREATE PROCEDURE sp_UpdateUserPassword(
    IN p_user_id INT,
    IN p_password VARCHAR(255)
)
BEGIN
    UPDATE users SET password = p_password WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_MarkPasswordResetUsed;$
CREATE PROCEDURE sp_MarkPasswordResetUsed(
    IN p_reset_id INT
)
BEGIN
    UPDATE password_resets SET is_used = 1 WHERE id = p_reset_id;
END$$

-- ============================================
-- NOTIFICATION SERVICE PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_GetUserNotifications;$
CREATE PROCEDURE sp_GetUserNotifications(
    IN p_user_id INT,
    IN p_limit INT,
    IN p_unread_only BOOLEAN
)
BEGIN
    IF p_unread_only THEN
        SELECT id, title, message, type, reference_type, reference_id, is_read, created_at
        FROM notifications
        WHERE user_id = p_user_id AND is_read = 0
        ORDER BY created_at DESC LIMIT p_limit;
    ELSE
        SELECT id, title, message, type, reference_type, reference_id, is_read, created_at
        FROM notifications
        WHERE user_id = p_user_id
        ORDER BY created_at DESC LIMIT p_limit;
    END IF;
END$$

DROP PROCEDURE IF EXISTS sp_GetUnreadNotificationCount;$
CREATE PROCEDURE sp_GetUnreadNotificationCount(
    IN p_user_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM notifications WHERE user_id = p_user_id AND is_read = 0;
END$$

DROP PROCEDURE IF EXISTS sp_MarkNotificationAsRead;$
CREATE PROCEDURE sp_MarkNotificationAsRead(
    IN p_notification_id INT,
    IN p_user_id INT,
    IN p_read_at DATETIME
)
BEGIN
    UPDATE notifications SET is_read = 1, read_at = p_read_at 
    WHERE id = p_notification_id AND user_id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_MarkAllNotificationsAsRead;$
CREATE PROCEDURE sp_MarkAllNotificationsAsRead(
    IN p_user_id INT,
    IN p_read_at DATETIME
)
BEGIN
    UPDATE notifications SET is_read = 1, read_at = p_read_at 
    WHERE user_id = p_user_id AND is_read = 0;
END$$

DROP PROCEDURE IF EXISTS sp_CreateNotification;$
CREATE PROCEDURE sp_CreateNotification(
    IN p_user_id INT,
    IN p_title VARCHAR(255),
    IN p_message TEXT,
    IN p_type VARCHAR(50),
    IN p_reference_type VARCHAR(50),
    IN p_reference_id INT,
    IN p_created_at DATETIME
)
BEGIN
    INSERT INTO notifications (user_id, title, message, type, reference_type, reference_id, created_at)
    VALUES (p_user_id, p_title, p_message, p_type, p_reference_type, p_reference_id, p_created_at);
END$$

DROP PROCEDURE IF EXISTS sp_GetAdminUsers;$
CREATE PROCEDURE sp_GetAdminUsers()
BEGIN
    SELECT id FROM users WHERE role_id = 1 AND is_active = 1;
END$$

DROP PROCEDURE IF EXISTS sp_CleanupOldNotifications;$
CREATE PROCEDURE sp_CleanupOldNotifications(
    IN p_days INT
)
BEGIN
    DELETE FROM notifications 
    WHERE created_at < DATE_SUB(NOW(), INTERVAL p_days DAY) AND is_read = 1;
END$$

-- ============================================
-- COMMENT SERVICE PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_GetTaskComments;$
CREATE PROCEDURE sp_GetTaskComments(
    IN p_task_id INT
)
BEGIN
    SELECT c.id, c.task_id, c.user_id, c.comment, c.comment_type, c.is_internal, c.parent_comment_id, c.created_at,
           CONCAT(u.first_name, ' ', u.last_name) as user_name, u.profile_image
    FROM task_comments c
    JOIN users u ON c.user_id = u.id
    WHERE c.task_id = p_task_id AND c.parent_comment_id IS NULL
    ORDER BY c.created_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetCommentReplies;$
CREATE PROCEDURE sp_GetCommentReplies(
    IN p_parent_comment_id INT
)
BEGIN
    SELECT c.id, c.task_id, c.user_id, c.comment, c.comment_type, c.is_internal, c.created_at,
           CONCAT(u.first_name, ' ', u.last_name) as user_name, u.profile_image
    FROM task_comments c
    JOIN users u ON c.user_id = u.id
    WHERE c.parent_comment_id = p_parent_comment_id
    ORDER BY c.created_at;
END$$

DROP PROCEDURE IF EXISTS sp_AddComment;$
CREATE PROCEDURE sp_AddComment(
    IN p_task_id INT,
    IN p_user_id INT,
    IN p_comment TEXT,
    IN p_comment_type VARCHAR(50),
    IN p_is_internal BOOLEAN,
    IN p_parent_comment_id INT,
    IN p_created_at DATETIME
)
BEGIN
    INSERT INTO task_comments (task_id, user_id, comment, comment_type, is_internal, parent_comment_id, created_at)
    VALUES (p_task_id, p_user_id, p_comment, p_comment_type, p_is_internal, p_parent_comment_id, p_created_at);
    
    SELECT LAST_INSERT_ID() as id;
    
    UPDATE tasks SET comments_count = comments_count + 1 WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateTaskStatus;$
CREATE PROCEDURE sp_UpdateTaskStatus(
    IN p_task_id INT,
    IN p_status_id INT,
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE tasks SET status_id = p_status_id, updated_at = p_updated_at WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateTaskProgress;$
CREATE PROCEDURE sp_UpdateTaskProgress(
    IN p_task_id INT,
    IN p_progress INT,
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE tasks SET progress = p_progress, updated_at = p_updated_at WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskProjectId;$
CREATE PROCEDURE sp_GetTaskProjectId(
    IN p_task_id INT
)
BEGIN
    SELECT project_id FROM tasks WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_LogTime;$
CREATE PROCEDURE sp_LogTime(
    IN p_task_id INT,
    IN p_user_id INT,
    IN p_project_id INT,
    IN p_hours DECIMAL(5,2),
    IN p_log_date DATE,
    IN p_description TEXT,
    IN p_is_billable BOOLEAN,
    IN p_created_at DATETIME
)
BEGIN
    INSERT INTO time_logs (task_id, user_id, project_id, hours_logged, log_date, description, is_billable, created_at)
    VALUES (p_task_id, p_user_id, p_project_id, p_hours, p_log_date, p_description, p_is_billable, p_created_at);
    
    SELECT LAST_INSERT_ID() as id;
    
    UPDATE tasks SET actual_hours = actual_hours + p_hours, updated_at = p_created_at WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskTimeLogs;$
CREATE PROCEDURE sp_GetTaskTimeLogs(
    IN p_task_id INT
)
BEGIN
    SELECT tl.id, tl.task_id, tl.user_id, tl.hours_logged, tl.log_date, tl.description, tl.is_billable, tl.is_approved,
           CONCAT(u.first_name, ' ', u.last_name) as user_name,
           CONCAT(a.first_name, ' ', a.last_name) as approver_name
    FROM time_logs tl
    JOIN users u ON tl.user_id = u.id
    LEFT JOIN users a ON tl.approved_by = a.id
    WHERE tl.task_id = p_task_id
    ORDER BY tl.log_date DESC;
END$$

-- ============================================
-- ACTIVITY LOG SERVICE PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_LogActivity;$
CREATE PROCEDURE sp_LogActivity(
    IN p_task_id INT,
    IN p_user_id INT,
    IN p_action VARCHAR(100),
    IN p_field_changed VARCHAR(100),
    IN p_old_value TEXT,
    IN p_new_value TEXT,
    IN p_ip_address VARCHAR(50),
    IN p_created_at DATETIME
)
BEGIN
    INSERT INTO task_activity_log (task_id, user_id, action, field_changed, old_value, new_value, ip_address, created_at)
    VALUES (p_task_id, p_user_id, p_action, p_field_changed, p_old_value, p_new_value, p_ip_address, p_created_at);
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskActivityLog;$
CREATE PROCEDURE sp_GetTaskActivityLog(
    IN p_task_id INT
)
BEGIN
    SELECT al.id, al.task_id, al.action, al.field_changed, al.old_value, al.new_value, al.created_at,
           CONCAT(u.first_name, ' ', u.last_name) as user_name, u.profile_image
    FROM task_activity_log al
    JOIN users u ON al.user_id = u.id
    WHERE al.task_id = p_task_id
    ORDER BY al.created_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetRecentActivity;$
CREATE PROCEDURE sp_GetRecentActivity(
    IN p_user_id INT,
    IN p_limit INT
)
BEGIN
    SELECT al.id, al.task_id, al.action, al.field_changed, al.old_value, al.new_value, al.created_at,
           CONCAT(u.first_name, ' ', u.last_name) as user_name, u.profile_image
    FROM task_activity_log al
    JOIN users u ON al.user_id = u.id
    JOIN tasks t ON al.task_id = t.id
    WHERE t.assigned_to = p_user_id OR t.created_by = p_user_id
    ORDER BY al.created_at DESC
    LIMIT p_limit;
END$$

DELIMITER ;

-- Success message
SELECT 'All stored procedures created successfully!' as Status;

-- ============================================
-- DASHBOARD SERVICE PROCEDURES
-- ============================================

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GetEmployeeCount;$
CREATE PROCEDURE sp_GetEmployeeCount()
BEGIN
    SELECT COUNT(*) as count FROM users WHERE is_active = 1 AND role_id = 3;
END$$

DROP PROCEDURE IF EXISTS sp_GetActiveProjectsCount;$
CREATE PROCEDURE sp_GetActiveProjectsCount()
BEGIN
    SELECT COUNT(*) as count FROM projects WHERE is_active = 1 AND status IN ('planning', 'active');
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskStatistics;$
CREATE PROCEDURE sp_GetTaskStatistics()
BEGIN
    SELECT 
        COUNT(*) as total,
        SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) as completed,
        SUM(CASE WHEN s.name NOT IN ('completed', 'cancelled') THEN 1 ELSE 0 END) as pending,
        SUM(CASE WHEN s.name IN ('in_progress') THEN 1 ELSE 0 END) as in_progress,
        SUM(CASE WHEN s.name IN ('in_review', 'testing') THEN 1 ELSE 0 END) as in_review,
        SUM(CASE WHEN due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled') THEN 1 ELSE 0 END) as overdue
    FROM tasks t
    JOIN statuses s ON t.status_id = s.id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTotalHoursLogged;$
CREATE PROCEDURE sp_GetTotalHoursLogged()
BEGIN
    SELECT COALESCE(SUM(hours_logged), 0) as total_hours FROM time_logs;
END$$

DROP PROCEDURE IF EXISTS sp_GetTasksByStatus;$
CREATE PROCEDURE sp_GetTasksByStatus()
BEGIN
    SELECT s.name, COUNT(t.id) as count
    FROM statuses s
    LEFT JOIN tasks t ON t.status_id = s.id
    GROUP BY s.id, s.name
    ORDER BY s.id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTasksByPriority;$
CREATE PROCEDURE sp_GetTasksByPriority()
BEGIN
    SELECT p.name, COUNT(t.id) as count
    FROM priorities p
    LEFT JOIN tasks t ON t.priority_id = p.id
    GROUP BY p.id, p.name
    ORDER BY p.id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTasksByDepartment;$
CREATE PROCEDURE sp_GetTasksByDepartment()
BEGIN
    SELECT d.name, COUNT(t.id) as count
    FROM departments d
    LEFT JOIN users u ON u.department_id = d.id
    LEFT JOIN tasks t ON t.assigned_to = u.id
    WHERE d.is_active = 1
    GROUP BY d.id, d.name
    ORDER BY count DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetMonthlyTrends;$
CREATE PROCEDURE sp_GetMonthlyTrends()
BEGIN
    SELECT 
        DATE_FORMAT(created_at, '%Y-%m') as month,
        COUNT(*) as created,
        SUM(CASE WHEN status_id = (SELECT id FROM statuses WHERE name = 'completed') THEN 1 ELSE 0 END) as completed
    FROM tasks
    WHERE created_at >= DATE_SUB(CURDATE(), INTERVAL 6 MONTH)
    GROUP BY DATE_FORMAT(created_at, '%Y-%m')
    ORDER BY month;
END$$

DROP PROCEDURE IF EXISTS sp_GetTopPerformers;$
CREATE PROCEDURE sp_GetTopPerformers()
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.profile_image, u.job_title, d.name as department_name,
           COALESCE(COUNT(t.id), 0) as total, 
           COALESCE(SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END), 0) as completed
    FROM users u
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN tasks t ON t.assigned_to = u.id
    LEFT JOIN statuses s ON t.status_id = s.id
    WHERE u.is_active = 1 AND u.role_id = 3
    GROUP BY u.id
    ORDER BY completed DESC
    LIMIT 5;
END$$

DROP PROCEDURE IF EXISTS sp_GetActiveProjects;$
CREATE PROCEDURE sp_GetActiveProjects()
BEGIN
    SELECT p.id, p.name, p.code, p.status, p.priority, p.end_date,
           CONCAT(m.first_name, ' ', m.last_name) as manager_name,
           COALESCE(COUNT(t.id), 0) as total_tasks,
           COALESCE(SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END), 0) as completed_tasks
    FROM projects p
    LEFT JOIN users m ON p.manager_id = m.id
    LEFT JOIN tasks t ON t.project_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    WHERE p.is_active = 1 AND p.status IN ('planning', 'active')
    GROUP BY p.id
    ORDER BY p.created_at DESC
    LIMIT 5;
END$$

DROP PROCEDURE IF EXISTS sp_GetRecentTasks;$
CREATE PROCEDURE sp_GetRecentTasks()
BEGIN
    SELECT t.id, t.title, t.due_date, p.name as priority_name, s.name as status_name
    FROM tasks t
    JOIN priorities p ON t.priority_id = p.id
    JOIN statuses s ON t.status_id = s.id
    ORDER BY t.created_at DESC
    LIMIT 5;
END$$

DROP PROCEDURE IF EXISTS sp_GetOverdueTasks;$
CREATE PROCEDURE sp_GetOverdueTasks()
BEGIN
    SELECT t.id, t.title, t.due_date, p.name as priority_name, s.name as status_name,
           CONCAT(u.first_name, ' ', u.last_name) as assignee_name
    FROM tasks t
    JOIN priorities p ON t.priority_id = p.id
    JOIN statuses s ON t.status_id = s.id
    LEFT JOIN users u ON t.assigned_to = u.id
    WHERE t.due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled')
    ORDER BY t.due_date ASC
    LIMIT 5;
END$$

DROP PROCEDURE IF EXISTS sp_GetEmployeeDashboard;$
CREATE PROCEDURE sp_GetEmployeeDashboard(
    IN p_user_id INT
)
BEGIN
    -- User details
    SELECT u.job_title, d.name as department_name, t.name as team_name
    FROM users u
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    WHERE u.id = p_user_id;
    
    -- Task statistics
    SELECT 
        COUNT(*) as total,
        SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) as completed,
        SUM(CASE WHEN s.name IN ('in_progress', 'in_review', 'testing') THEN 1 ELSE 0 END) as in_progress,
        SUM(CASE WHEN due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled') THEN 1 ELSE 0 END) as overdue
    FROM tasks t
    JOIN statuses s ON t.status_id = s.id
    WHERE t.assigned_to = p_user_id;
    
    -- Hours this week
    SELECT COALESCE(SUM(hours_logged), 0) as hours_this_week 
    FROM time_logs 
    WHERE user_id = p_user_id AND log_date >= DATE_SUB(CURDATE(), INTERVAL WEEKDAY(CURDATE()) DAY);
    
    -- Hours this month
    SELECT COALESCE(SUM(hours_logged), 0) as hours_this_month 
    FROM time_logs 
    WHERE user_id = p_user_id AND MONTH(log_date) = MONTH(CURDATE()) AND YEAR(log_date) = YEAR(CURDATE());
    
    -- Today's tasks
    SELECT t.id, t.title, t.due_date, p.name as priority_name, s.name as status_name
    FROM tasks t
    JOIN priorities p ON t.priority_id = p.id
    JOIN statuses s ON t.status_id = s.id
    WHERE t.assigned_to = p_user_id AND t.due_date = CURDATE() AND s.name NOT IN ('completed', 'cancelled')
    ORDER BY p.level DESC;
    
    -- Upcoming tasks
    SELECT t.id, t.title, t.due_date, p.name as priority_name, s.name as status_name
    FROM tasks t
    JOIN priorities p ON t.priority_id = p.id
    JOIN statuses s ON t.status_id = s.id
    WHERE t.assigned_to = p_user_id 
      AND t.due_date > CURDATE() 
      AND t.due_date <= DATE_ADD(CURDATE(), INTERVAL 7 DAY)
      AND s.name NOT IN ('completed', 'cancelled')
    ORDER BY t.due_date ASC
    LIMIT 5;
    
    -- Overdue tasks
    SELECT t.id, t.title, t.due_date, p.name as priority_name, s.name as status_name
    FROM tasks t
    JOIN priorities p ON t.priority_id = p.id
    JOIN statuses s ON t.status_id = s.id
    WHERE t.assigned_to = p_user_id AND t.due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled')
    ORDER BY t.due_date ASC;
    
    -- Recently completed
    SELECT t.id, t.title, t.completed_date as due_date, p.name as priority_name, s.name as status_name
    FROM tasks t
    JOIN priorities p ON t.priority_id = p.id
    JOIN statuses s ON t.status_id = s.id
    WHERE t.assigned_to = p_user_id AND s.name = 'completed'
    ORDER BY t.completed_date DESC
    LIMIT 5;
END$$

-- ============================================
-- TASK SERVICE PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_GetTasks;$
CREATE PROCEDURE sp_GetTasks(
    IN p_user_id INT,
    IN p_status_id INT,
    IN p_priority_id INT,
    IN p_category_id INT
)
BEGIN
    SELECT t.id, t.title, t.description, t.due_date, c.name as category_name, p.name as priority_name, s.name as status_name
    FROM tasks t
    LEFT JOIN categories c ON t.category_id = c.id
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    WHERE (t.created_by = p_user_id OR t.assigned_to = p_user_id)
      AND (p_status_id IS NULL OR t.status_id = p_status_id)
      AND (p_priority_id IS NULL OR t.priority_id = p_priority_id)
      AND (p_category_id IS NULL OR t.category_id = p_category_id)
    ORDER BY t.created_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskById;$
CREATE PROCEDURE sp_GetTaskById(
    IN p_task_id INT
)
BEGIN
    SELECT t.id, t.title, t.description, t.due_date, t.created_at, t.updated_at,
           t.created_by, t.assigned_to, t.hourly_rate,
           c.name as category_name, p.name as priority_name, s.name as status_name,
           u.first_name as creator_first, u.last_name as creator_last
    FROM tasks t
    LEFT JOIN categories c ON t.category_id = c.id
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN users u ON t.created_by = u.id
    WHERE t.id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_CreateTask;$
CREATE PROCEDURE sp_CreateTask(
    IN p_title VARCHAR(255),
    IN p_description TEXT,
    IN p_project_id INT,
    IN p_category_id INT,
    IN p_priority_id INT,
    IN p_status_id INT,
    IN p_due_date DATE,
    IN p_estimated_hours DECIMAL(5,2),
    IN p_hourly_rate DECIMAL(10,2),
    IN p_created_by INT,
    IN p_created_at DATETIME,
    IN p_updated_at DATETIME
)
BEGIN
    INSERT INTO tasks (title, description, project_id, category_id, priority_id, status_id, 
                      due_date, estimated_hours, hourly_rate, created_by, created_at, updated_at)
    VALUES (p_title, p_description, p_project_id, p_category_id, p_priority_id, p_status_id, 
            p_due_date, p_estimated_hours, p_hourly_rate, p_created_by, p_created_at, p_updated_at);
    
    SELECT LAST_INSERT_ID() as id;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateTask;$
CREATE PROCEDURE sp_UpdateTask(
    IN p_task_id INT,
    IN p_title VARCHAR(255),
    IN p_description TEXT,
    IN p_category_id INT,
    IN p_priority_id INT,
    IN p_status_id INT,
    IN p_due_date DATE,
    IN p_hourly_rate DECIMAL(10,2),
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE tasks 
    SET title = p_title, 
        description = p_description, 
        category_id = p_category_id, 
        priority_id = p_priority_id, 
        status_id = p_status_id, 
        due_date = p_due_date,
        hourly_rate = p_hourly_rate, 
        updated_at = p_updated_at
    WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_DeleteTask;$
CREATE PROCEDURE sp_DeleteTask(
    IN p_task_id INT
)
BEGIN
    DELETE FROM tasks WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetCategories;$
CREATE PROCEDURE sp_GetCategories()
BEGIN
    SELECT id, name, description, color, icon, is_active 
    FROM categories 
    WHERE is_active = 1;
END$$

DROP PROCEDURE IF EXISTS sp_GetPriorities;$
CREATE PROCEDURE sp_GetPriorities()
BEGIN
    SELECT id, name, level, color FROM priorities;
END$$

DROP PROCEDURE IF EXISTS sp_GetStatuses;$
CREATE PROCEDURE sp_GetStatuses()
BEGIN
    SELECT id, name, display_name, color, sort_order 
    FROM statuses 
    ORDER BY sort_order;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskAttachments;$
CREATE PROCEDURE sp_GetTaskAttachments(
    IN p_task_id INT
)
BEGIN
    SELECT ta.id, ta.file_name, ta.original_name, ta.file_type, ta.file_size, 
           ta.file_path, ta.created_at, CONCAT(u.first_name, ' ', u.last_name) as uploaded_by_name
    FROM task_attachments ta
    LEFT JOIN users u ON ta.uploaded_by = u.id
    WHERE ta.task_id = p_task_id
    ORDER BY ta.created_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_SaveAttachment;$
CREATE PROCEDURE sp_SaveAttachment(
    IN p_task_id INT,
    IN p_uploaded_by INT,
    IN p_file_name VARCHAR(255),
    IN p_original_name VARCHAR(255),
    IN p_file_type VARCHAR(50),
    IN p_file_size BIGINT,
    IN p_file_content LONGBLOB,
    IN p_content_type VARCHAR(100),
    IN p_created_at DATETIME
)
BEGIN
    INSERT INTO task_attachments 
        (task_id, uploaded_by, file_name, original_name, file_type, file_size, file_content, content_type, created_at, file_path)
    VALUES 
        (p_task_id, p_uploaded_by, p_file_name, p_original_name, p_file_type, p_file_size, p_file_content, p_content_type, p_created_at, 'BLOB_STORAGE');
    SELECT LAST_INSERT_ID() AS new_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetAttachmentPath;$
CREATE PROCEDURE sp_GetAttachmentPath(
    IN p_attachment_id INT
)
BEGIN
    SELECT file_path FROM task_attachments WHERE id = p_attachment_id;
END$$

DROP PROCEDURE IF EXISTS sp_DeleteAttachment;$
CREATE PROCEDURE sp_DeleteAttachment(
    IN p_attachment_id INT
)
BEGIN
    DELETE FROM task_attachments WHERE id = p_attachment_id;
END$$

DELIMITER ;

-- ============================================
-- USER SERVICE PROCEDURES
-- ============================================

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GetUsers;$
CREATE PROCEDURE sp_GetUsers(
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_team_id INT,
    IN p_is_active BOOLEAN
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.email, u.employee_id, u.job_title, u.profile_image, u.is_active, u.last_login,
           r.name as role_name, d.name as department_name, t.name as team_name,
           (SELECT COUNT(*) FROM tasks WHERE assigned_to = u.id) as task_count
    FROM users u
    LEFT JOIN roles r ON u.role_id = r.id
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    WHERE (p_role_id IS NULL OR u.role_id = p_role_id)
      AND (p_department_id IS NULL OR u.department_id = p_department_id)
      AND (p_team_id IS NULL OR u.team_id = p_team_id)
      AND (p_is_active IS NULL OR u.is_active = p_is_active)
    ORDER BY u.first_name, u.last_name;
END$$

DROP PROCEDURE IF EXISTS sp_GetUserForm;$
CREATE PROCEDURE sp_GetUserForm(
    IN p_user_id INT
)
BEGIN
    SELECT id, first_name, last_name, email, phone, employee_id, job_title, 
           role_id, department_id, team_id, reports_to, hire_date, is_active
    FROM users WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetRoles;$
CREATE PROCEDURE sp_GetRoles()
BEGIN
    SELECT id, name FROM roles WHERE id != 1 ORDER BY id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTeams;$
CREATE PROCEDURE sp_GetTeams()
BEGIN
    SELECT id, name FROM teams WHERE is_active = 1 ORDER BY name;
END$$

DROP PROCEDURE IF EXISTS sp_GetManagers;$
CREATE PROCEDURE sp_GetManagers()
BEGIN
    SELECT u.id, CONCAT(u.first_name, ' ', u.last_name) as full_name 
    FROM users u
    JOIN roles r ON u.role_id = r.id
    WHERE u.is_active = 1 AND (r.name = 'Admin' OR r.name = 'Manager')
    ORDER BY u.first_name;
END$$

DROP PROCEDURE IF EXISTS sp_GenerateEmployeeId;$
CREATE PROCEDURE sp_GenerateEmployeeId()
BEGIN
    SELECT employee_id FROM users 
    WHERE employee_id LIKE 'EMP%' 
    ORDER BY CAST(SUBSTRING(employee_id, 4) AS UNSIGNED) DESC 
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS sp_CreateUser;$
CREATE PROCEDURE sp_CreateUser(
    IN p_first_name VARCHAR(100),
    IN p_last_name VARCHAR(100),
    IN p_email VARCHAR(255),
    IN p_password VARCHAR(255),
    IN p_phone VARCHAR(20),
    IN p_employee_id VARCHAR(50),
    IN p_job_title VARCHAR(100),
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_team_id INT,
    IN p_reports_to INT,
    IN p_hire_date DATE,
    IN p_is_active BOOLEAN,
    IN p_created_at DATETIME
)
BEGIN
    INSERT INTO users (first_name, last_name, email, password, phone, employee_id, job_title, 
                      role_id, department_id, team_id, reports_to, hire_date, is_active, is_verified, created_at)
    VALUES (p_first_name, p_last_name, p_email, p_password, p_phone, p_employee_id, p_job_title, 
            p_role_id, p_department_id, p_team_id, p_reports_to, p_hire_date, p_is_active, 1, p_created_at);
    
    SELECT LAST_INSERT_ID() as id;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateUser;$
CREATE PROCEDURE sp_UpdateUser(
    IN p_user_id INT,
    IN p_first_name VARCHAR(100),
    IN p_last_name VARCHAR(100),
    IN p_email VARCHAR(255),
    IN p_phone VARCHAR(20),
    IN p_employee_id VARCHAR(50),
    IN p_job_title VARCHAR(100),
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_team_id INT,
    IN p_reports_to INT,
    IN p_hire_date DATE,
    IN p_is_active BOOLEAN
)
BEGIN
    UPDATE users SET 
        first_name = p_first_name, 
        last_name = p_last_name, 
        email = p_email, 
        phone = p_phone,
        employee_id = p_employee_id, 
        job_title = p_job_title, 
        role_id = p_role_id,
        department_id = p_department_id, 
        team_id = p_team_id, 
        reports_to = p_reports_to,
        hire_date = p_hire_date, 
        is_active = p_is_active
    WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateUserWithPassword;$
CREATE PROCEDURE sp_UpdateUserWithPassword(
    IN p_user_id INT,
    IN p_first_name VARCHAR(100),
    IN p_last_name VARCHAR(100),
    IN p_email VARCHAR(255),
    IN p_password VARCHAR(255),
    IN p_phone VARCHAR(20),
    IN p_employee_id VARCHAR(50),
    IN p_job_title VARCHAR(100),
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_team_id INT,
    IN p_reports_to INT,
    IN p_hire_date DATE,
    IN p_is_active BOOLEAN
)
BEGIN
    UPDATE users SET 
        first_name = p_first_name, 
        last_name = p_last_name, 
        email = p_email, 
        password = p_password,
        phone = p_phone,
        employee_id = p_employee_id, 
        job_title = p_job_title, 
        role_id = p_role_id,
        department_id = p_department_id, 
        team_id = p_team_id, 
        reports_to = p_reports_to,
        hire_date = p_hire_date, 
        is_active = p_is_active
    WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_CheckUserEmail;$
CREATE PROCEDURE sp_CheckUserEmail(
    IN p_user_id INT
)
BEGIN
    SELECT email FROM users WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_UnassignUserTasks;$
CREATE PROCEDURE sp_UnassignUserTasks(
    IN p_user_id INT
)
BEGIN
    UPDATE tasks SET assigned_to = NULL WHERE assigned_to = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_DeleteUser;$
CREATE PROCEDURE sp_DeleteUser(
    IN p_user_id INT
)
BEGIN
    UPDATE users SET is_active = 0 WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetEmployeesForAssignment;$
CREATE PROCEDURE sp_GetEmployeesForAssignment()
BEGIN
    SELECT id, CONCAT(first_name, ' ', last_name) as full_name, job_title
    FROM users 
    WHERE is_active = 1 AND role_id != 1 
    ORDER BY first_name, last_name;
END$$

DROP PROCEDURE IF EXISTS sp_GetEmployeeSummaries;$
CREATE PROCEDURE sp_GetEmployeeSummaries(
    IN p_department_id INT,
    IN p_team_id INT
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.profile_image, u.job_title,
           d.name as department_name, t.name as team_name,
           COUNT(DISTINCT tk.id) as total_tasks,
           COUNT(DISTINCT CASE WHEN s.name = 'completed' THEN tk.id END) as completed_tasks,
           COUNT(DISTINCT CASE WHEN s.name IN ('in_progress', 'in_review', 'testing') THEN tk.id END) as in_progress_tasks,
           COUNT(DISTINCT CASE WHEN tk.due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled') THEN tk.id END) as overdue_tasks,
           COALESCE(SUM(DISTINCT tl.hours_logged), 0) as hours_logged
    FROM users u
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    LEFT JOIN tasks tk ON tk.assigned_to = u.id
    LEFT JOIN statuses s ON tk.status_id = s.id
    LEFT JOIN time_logs tl ON tl.user_id = u.id
    WHERE u.is_active = 1 AND u.role_id != 1
      AND (p_department_id IS NULL OR u.department_id = p_department_id)
      AND (p_team_id IS NULL OR u.team_id = p_team_id)
    GROUP BY u.id 
    ORDER BY completed_tasks DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetUserProfile;$
CREATE PROCEDURE sp_GetUserProfile(
    IN p_user_id INT
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.email, u.phone, u.employee_id, u.job_title, u.profile_image, u.hire_date,
           d.name as department_name, t.name as team_name, r.name as role_name,
           CONCAT(m.first_name, ' ', m.last_name) as manager_name,
           COUNT(DISTINCT tk.id) as total_tasks,
           SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) as completed_tasks,
           SUM(CASE WHEN tk.due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled') THEN 1 ELSE 0 END) as overdue_tasks,
           COALESCE(SUM(tl.hours_logged), 0) as hours_logged
    FROM users u
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    LEFT JOIN roles r ON u.role_id = r.id
    LEFT JOIN users m ON u.reports_to = m.id
    LEFT JOIN tasks tk ON tk.assigned_to = u.id
    LEFT JOIN statuses s ON tk.status_id = s.id
    LEFT JOIN time_logs tl ON tl.user_id = u.id
    WHERE u.id = p_user_id
    GROUP BY u.id;
END$$

DROP PROCEDURE IF EXISTS sp_VerifyUserPassword;$
CREATE PROCEDURE sp_VerifyUserPassword(
    IN p_user_id INT
)
BEGIN
    SELECT password FROM users WHERE id = p_user_id AND is_active = 1;
END$$

DROP PROCEDURE IF EXISTS sp_ChangeUserPassword;$
CREATE PROCEDURE sp_ChangeUserPassword(
    IN p_user_id INT,
    IN p_new_password VARCHAR(255)
)
BEGIN
    UPDATE users SET password = p_new_password WHERE id = p_user_id;
END$$

-- ============================================
-- PROJECT SERVICE PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_GetProjects;$
CREATE PROCEDURE sp_GetProjects(
    IN p_status VARCHAR(50),
    IN p_department_id INT,
    IN p_priority VARCHAR(50)
)
BEGIN
    SELECT p.id, p.name, p.code, p.status, p.priority, p.start_date, p.end_date,
           CONCAT(m.first_name, ' ', m.last_name) as manager_name,
           COUNT(DISTINCT t.id) as total_tasks,
           COALESCE(SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END), 0) as completed_tasks,
           COUNT(DISTINCT t.assigned_to) as team_size
    FROM projects p
    LEFT JOIN users m ON p.manager_id = m.id
    LEFT JOIN tasks t ON t.project_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    WHERE p.is_active = 1
      AND (p_status IS NULL OR p.status = p_status)
      AND (p_department_id IS NULL OR p.department_id = p_department_id)
      AND (p_priority IS NULL OR p.priority = p_priority)
    GROUP BY p.id 
    ORDER BY p.created_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetProjectForm;$
CREATE PROCEDURE sp_GetProjectForm(
    IN p_project_id INT
)
BEGIN
    SELECT id, name, description, code, department_id, manager_id, 
           start_date, end_date, budget, status, priority
    FROM projects WHERE id = p_project_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetProjectDetails;$
CREATE PROCEDURE sp_GetProjectDetails(
    IN p_project_id INT
)
BEGIN
    -- Project info
    SELECT p.id, p.name, p.description, p.code, p.start_date, p.end_date, p.budget, p.status, p.priority, p.created_at,
           p.manager_id, d.name as department_name, CONCAT(m.first_name, ' ', m.last_name) as manager_name
    FROM projects p
    LEFT JOIN departments d ON p.department_id = d.id
    LEFT JOIN users m ON p.manager_id = m.id
    WHERE p.id = p_project_id;
    
    -- Task statistics
    SELECT 
        COUNT(*) as total,
        SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) as completed,
        SUM(CASE WHEN s.name IN ('in_progress', 'in_review', 'testing') THEN 1 ELSE 0 END) as in_progress,
        SUM(CASE WHEN due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled') THEN 1 ELSE 0 END) as overdue
    FROM tasks t
    JOIN statuses s ON t.status_id = s.id
    WHERE t.project_id = p_project_id;
    
    -- Total hours
    SELECT COALESCE(SUM(hours_logged), 0) as total_hours 
    FROM time_logs 
    WHERE project_id = p_project_id;
    
    -- Project tasks
    SELECT t.id, t.title, t.description, t.due_date,
           p.name as priority_name, s.name as status_name, c.name as category_name
    FROM tasks t
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN categories c ON t.category_id = c.id
    WHERE t.project_id = p_project_id
    ORDER BY t.due_date;
END$$

DROP PROCEDURE IF EXISTS sp_GenerateProjectCode;$
CREATE PROCEDURE sp_GenerateProjectCode()
BEGIN
    SELECT code FROM projects 
    WHERE code LIKE 'PRJ%' 
    ORDER BY CAST(SUBSTRING(code, 4) AS UNSIGNED) DESC 
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS sp_CheckProjectCodeExists;$
CREATE PROCEDURE sp_CheckProjectCodeExists(
    IN p_code VARCHAR(50),
    IN p_exclude_id INT
)
BEGIN
    IF p_exclude_id IS NULL THEN
        SELECT COUNT(*) as count FROM projects WHERE code = p_code;
    ELSE
        SELECT COUNT(*) as count FROM projects WHERE code = p_code AND id != p_exclude_id;
    END IF;
END$$

DROP PROCEDURE IF EXISTS sp_CreateProject;$
CREATE PROCEDURE sp_CreateProject(
    IN p_name VARCHAR(255),
    IN p_description TEXT,
    IN p_code VARCHAR(50),
    IN p_department_id INT,
    IN p_manager_id INT,
    IN p_start_date DATE,
    IN p_end_date DATE,
    IN p_budget DECIMAL(15,2),
    IN p_status VARCHAR(50),
    IN p_priority VARCHAR(50),
    IN p_created_by INT,
    IN p_created_at DATETIME
)
BEGIN
    INSERT INTO projects (name, description, code, department_id, manager_id, start_date, end_date, 
                         budget, status, priority, created_by, created_at)
    VALUES (p_name, p_description, p_code, p_department_id, p_manager_id, p_start_date, p_end_date,
            p_budget, p_status, p_priority, p_created_by, p_created_at);
    
    SELECT LAST_INSERT_ID() as id;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateProject;$
CREATE PROCEDURE sp_UpdateProject(
    IN p_project_id INT,
    IN p_name VARCHAR(255),
    IN p_description TEXT,
    IN p_code VARCHAR(50),
    IN p_department_id INT,
    IN p_manager_id INT,
    IN p_start_date DATE,
    IN p_end_date DATE,
    IN p_budget DECIMAL(15,2),
    IN p_status VARCHAR(50),
    IN p_priority VARCHAR(50),
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE projects SET 
        name = p_name, 
        description = p_description, 
        code = p_code,
        department_id = p_department_id, 
        manager_id = p_manager_id,
        start_date = p_start_date, 
        end_date = p_end_date,
        budget = p_budget, 
        status = p_status, 
        priority = p_priority,
        updated_at = p_updated_at
    WHERE id = p_project_id;
END$$

DROP PROCEDURE IF EXISTS sp_DeleteProject;$
CREATE PROCEDURE sp_DeleteProject(
    IN p_project_id INT
)
BEGIN
    -- Unassign tasks
    UPDATE tasks SET project_id = NULL WHERE project_id = p_project_id;
    
    -- Soft delete project
    UPDATE projects SET is_active = 0 WHERE id = p_project_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetProjectsForSelect;$
CREATE PROCEDURE sp_GetProjectsForSelect()
BEGIN
    SELECT id, name FROM projects WHERE is_active = 1 AND status != 'completed' ORDER BY name;
END$$

DELIMITER ;

-- ============================================
-- PENDING USER SERVICE PROCEDURES
-- ============================================

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GetPendingUsers;$
CREATE PROCEDURE sp_GetPendingUsers(
    IN p_status_filter VARCHAR(50)
)
BEGIN
    SELECT pu.id, pu.first_name, pu.last_name, pu.email, pu.phone, pu.job_title, pu.status,
           pu.requested_at, pu.reviewed_at, pu.rejection_reason,
           d.name as department_name,
           CONCAT(u.first_name, ' ', u.last_name) as reviewer_name
    FROM pending_users pu
    LEFT JOIN departments d ON pu.department_id = d.id
    LEFT JOIN users u ON pu.reviewed_by = u.id
    WHERE (p_status_filter IS NULL OR pu.status = p_status_filter)
    ORDER BY pu.requested_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetPendingUserForApproval;$
CREATE PROCEDURE sp_GetPendingUserForApproval(
    IN p_pending_id INT
)
BEGIN
    SELECT id, first_name, last_name, email, phone, job_title, department_id
    FROM pending_users WHERE id = p_pending_id AND status = 'pending';
END$$

DROP PROCEDURE IF EXISTS sp_GetPendingUserData;$
CREATE PROCEDURE sp_GetPendingUserData(
    IN p_pending_id INT
)
BEGIN
    SELECT * FROM pending_users WHERE id = p_pending_id AND status = 'pending';
END$$

DROP PROCEDURE IF EXISTS sp_ApprovePendingUser;$
CREATE PROCEDURE sp_ApprovePendingUser(
    IN p_first_name VARCHAR(100),
    IN p_last_name VARCHAR(100),
    IN p_email VARCHAR(255),
    IN p_password VARCHAR(255),
    IN p_phone VARCHAR(20),
    IN p_employee_id VARCHAR(50),
    IN p_job_title VARCHAR(100),
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_team_id INT,
    IN p_reports_to INT,
    IN p_hire_date DATE,
    IN p_created_at DATETIME,
    IN p_pending_id INT,
    IN p_reviewed_at DATETIME,
    IN p_reviewed_by INT
)
BEGIN
    -- Create user account
    INSERT INTO users (first_name, last_name, email, password, phone, employee_id, job_title, 
                      role_id, department_id, team_id, reports_to, hire_date, 
                      is_active, is_verified, created_at)
    VALUES (p_first_name, p_last_name, p_email, p_password, p_phone, p_employee_id, p_job_title, 
            p_role_id, p_department_id, p_team_id, p_reports_to, p_hire_date, 
            1, 1, p_created_at);
    
    -- Update pending user status
    UPDATE pending_users 
    SET status = 'approved', reviewed_at = p_reviewed_at, reviewed_by = p_reviewed_by 
    WHERE id = p_pending_id;
END$$

DROP PROCEDURE IF EXISTS sp_RejectPendingUser;$
CREATE PROCEDURE sp_RejectPendingUser(
    IN p_pending_id INT,
    IN p_reviewed_at DATETIME,
    IN p_reviewed_by INT,
    IN p_rejection_reason TEXT
)
BEGIN
    UPDATE pending_users 
    SET status = 'rejected', reviewed_at = p_reviewed_at, reviewed_by = p_reviewed_by, rejection_reason = p_rejection_reason 
    WHERE id = p_pending_id AND status = 'pending';
END$$

DROP PROCEDURE IF EXISTS sp_GetPendingCount;$
CREATE PROCEDURE sp_GetPendingCount()
BEGIN
    SELECT COUNT(*) as count FROM pending_users WHERE status = 'pending';
END$$

-- ============================================
-- MANAGER SERVICE PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_GetManagerDepartment;$
CREATE PROCEDURE sp_GetManagerDepartment(
    IN p_manager_id INT
)
BEGIN
    SELECT department_id FROM users WHERE id = p_manager_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetDepartmentName;$
CREATE PROCEDURE sp_GetDepartmentName(
    IN p_department_id INT
)
BEGIN
    SELECT name FROM departments WHERE id = p_department_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTeamMemberCount;$
CREATE PROCEDURE sp_GetTeamMemberCount(
    IN p_department_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM users WHERE department_id = p_department_id AND is_active = 1;
END$$

DROP PROCEDURE IF EXISTS sp_GetActiveProjectsCountByManager;$
CREATE PROCEDURE sp_GetActiveProjectsCountByManager(
    IN p_manager_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM projects WHERE manager_id = p_manager_id AND is_active = 1 AND status IN ('planning', 'active');
END$$

DROP PROCEDURE IF EXISTS sp_GetProjectsOnTrackCount;$
CREATE PROCEDURE sp_GetProjectsOnTrackCount(
    IN p_manager_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM projects 
    WHERE manager_id = p_manager_id 
      AND is_active = 1 
      AND status = 'active'
      AND (end_date IS NULL OR end_date >= CURDATE());
END$$

DROP PROCEDURE IF EXISTS sp_GetProjectsAtRiskCount;$
CREATE PROCEDURE sp_GetProjectsAtRiskCount(
    IN p_manager_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM projects 
    WHERE manager_id = p_manager_id 
      AND is_active = 1 
      AND status = 'active'
      AND end_date IS NOT NULL
      AND DATEDIFF(end_date, CURDATE()) BETWEEN 1 AND 7;
END$$

DROP PROCEDURE IF EXISTS sp_GetProjectsDelayedCount;$
CREATE PROCEDURE sp_GetProjectsDelayedCount(
    IN p_manager_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM projects 
    WHERE manager_id = p_manager_id 
      AND is_active = 1 
      AND status = 'active'
      AND end_date < CURDATE();
END$$

DROP PROCEDURE IF EXISTS sp_GetTotalTasksCountByManager;$
CREATE PROCEDURE sp_GetTotalTasksCountByManager(
    IN p_manager_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM tasks t
    INNER JOIN projects p ON t.project_id = p.id
    WHERE p.manager_id = p_manager_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetCompletedThisWeekCount;$
CREATE PROCEDURE sp_GetCompletedThisWeekCount(
    IN p_manager_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM tasks t
    INNER JOIN projects p ON t.project_id = p.id
    WHERE p.manager_id = p_manager_id
      AND t.status_id = 4
      AND t.updated_at >= DATE_SUB(CURDATE(), INTERVAL 7 DAY);
END$$

DROP PROCEDURE IF EXISTS sp_GetOverdueTasksCountByManager;$
CREATE PROCEDURE sp_GetOverdueTasksCountByManager(
    IN p_manager_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM tasks t
    INNER JOIN projects p ON t.project_id = p.id
    WHERE p.manager_id = p_manager_id
      AND t.status_id != 4
      AND t.due_date < CURDATE();
END$$

DROP PROCEDURE IF EXISTS sp_GetInProgressTasksCount;$
CREATE PROCEDURE sp_GetInProgressTasksCount(
    IN p_manager_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM tasks t
    INNER JOIN projects p ON t.project_id = p.id
    WHERE p.manager_id = p_manager_id
      AND t.status_id IN (2, 3);
END$$

DROP PROCEDURE IF EXISTS sp_GetTeamCompletionRate;$
CREATE PROCEDURE sp_GetTeamCompletionRate(
    IN p_department_id INT
)
BEGIN
    SELECT 
        COALESCE(
            (SUM(CASE WHEN t.status_id = 4 THEN 1 ELSE 0 END) * 100.0 / COUNT(*)), 
            0
        ) as completion_rate
    FROM tasks t
    INNER JOIN users u ON t.assigned_to = u.id
    WHERE u.department_id = p_department_id
      AND t.created_at >= DATE_SUB(CURDATE(), INTERVAL 30 DAY);
END$$

DROP PROCEDURE IF EXISTS sp_GetAverageTaskTime;$
CREATE PROCEDURE sp_GetAverageTaskTime(
    IN p_department_id INT
)
BEGIN
    SELECT COALESCE(AVG(DATEDIFF(t.updated_at, t.created_at)), 0) as avg_days
    FROM tasks t
    INNER JOIN users u ON t.assigned_to = u.id
    WHERE u.department_id = p_department_id
      AND t.status_id = 4
      AND t.updated_at >= DATE_SUB(CURDATE(), INTERVAL 30 DAY);
END$$

DROP PROCEDURE IF EXISTS sp_GetRecentActivitiesByManager;$
CREATE PROCEDURE sp_GetRecentActivitiesByManager(
    IN p_manager_id INT,
    IN p_limit INT
)
BEGIN
    SELECT 
        CONCAT(u.first_name, ' ', u.last_name) as user_name,
        'Task' as entity_type,
        t.title as entity_name,
        CASE 
            WHEN t.status_id = 4 THEN 'completed'
            WHEN t.status_id = 3 THEN 'updated'
            ELSE 'created'
        END as action,
        t.updated_at as created_at
    FROM tasks t
    INNER JOIN users u ON t.assigned_to = u.id
    INNER JOIN projects p ON t.project_id = p.id
    WHERE p.manager_id = p_manager_id
    ORDER BY t.updated_at DESC
    LIMIT p_limit;
END$$

DROP PROCEDURE IF EXISTS sp_GetTopPerformersByDepartment;$
CREATE PROCEDURE sp_GetTopPerformersByDepartment(
    IN p_department_id INT,
    IN p_limit INT
)
BEGIN
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
    WHERE u.department_id = p_department_id
      AND u.is_active = 1
    GROUP BY u.id, u.first_name, u.last_name, u.employee_id
    HAVING total_tasks > 0
    ORDER BY completed_tasks DESC, on_time_tasks DESC
    LIMIT p_limit;
END$$

DROP PROCEDURE IF EXISTS sp_GetUpcomingDeadlinesByManager;$
CREATE PROCEDURE sp_GetUpcomingDeadlinesByManager(
    IN p_manager_id INT,
    IN p_limit INT
)
BEGIN
    SELECT 
        t.id as task_id,
        t.title as task_title,
        p.name as project_name,
        CONCAT(u.first_name, ' ', u.last_name) as assignee_name,
        t.due_date,
        pr.name as priority
    FROM tasks t
    INNER JOIN projects p ON t.project_id = p.id
    LEFT JOIN users u ON t.assigned_to = u.id
    LEFT JOIN priorities pr ON t.priority_id = pr.id
    WHERE p.manager_id = p_manager_id
      AND t.status_id != 4
      AND t.due_date >= CURDATE()
    ORDER BY t.due_date ASC
    LIMIT p_limit;
END$$

DROP PROCEDURE IF EXISTS sp_GetTeamMembers;$
CREATE PROCEDURE sp_GetTeamMembers(
    IN p_department_id INT,
    IN p_manager_id INT
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.email, u.employee_id,
           r.name as role_name, d.name as department_name, t.name as team_name,
           u.is_active, u.created_at,
           (SELECT COUNT(*) FROM tasks WHERE assigned_to = u.id AND status_id != 4) as active_tasks,
           (SELECT COUNT(*) FROM tasks WHERE assigned_to = u.id AND status_id = 4) as completed_tasks
    FROM users u
    LEFT JOIN roles r ON u.role_id = r.id
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    WHERE u.department_id = p_department_id 
      AND u.is_active = 1
      AND u.id != p_manager_id
    ORDER BY u.first_name, u.last_name;
END$$

DROP PROCEDURE IF EXISTS sp_GetManagedProjects;$
CREATE PROCEDURE sp_GetManagedProjects(
    IN p_manager_id INT,
    IN p_status VARCHAR(50)
)
BEGIN
    SELECT p.id, p.name, p.code, p.description, p.status, p.priority,
           p.start_date, p.end_date, p.budget,
           d.name as department_name,
           (SELECT COUNT(*) FROM tasks WHERE project_id = p.id) as total_tasks,
           (SELECT COUNT(*) FROM tasks WHERE project_id = p.id AND status_id = 4) as completed_tasks
    FROM projects p
    LEFT JOIN departments d ON p.department_id = d.id
    WHERE p.manager_id = p_manager_id
      AND p.is_active = 1
      AND (p_status IS NULL OR p.status = p_status)
    ORDER BY p.created_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetTeamPerformance;$
CREATE PROCEDURE sp_GetTeamPerformance(
    IN p_department_id INT,
    IN p_manager_id INT,
    IN p_start_date DATE,
    IN p_end_date DATE
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.employee_id,
           COUNT(t.id) as tasks_assigned,
           SUM(CASE WHEN t.status_id = 4 THEN 1 ELSE 0 END) as tasks_completed,
           SUM(CASE WHEN t.status_id IN (2, 3) THEN 1 ELSE 0 END) as tasks_in_progress,
           SUM(CASE WHEN t.due_date < CURDATE() AND t.status_id != 4 THEN 1 ELSE 0 END) as tasks_overdue
    FROM users u
    LEFT JOIN tasks t ON u.id = t.assigned_to 
        AND t.created_at BETWEEN p_start_date AND p_end_date
    WHERE u.department_id = p_department_id
      AND u.is_active = 1
      AND u.id != p_manager_id
    GROUP BY u.id, u.first_name, u.last_name, u.employee_id
    ORDER BY tasks_completed DESC;
END$$

-- ============================================
-- TASK SERVICE EXTENDED PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_GetTasksForAdmin;$
CREATE PROCEDURE sp_GetTasksForAdmin(
    IN p_status_id INT,
    IN p_priority_id INT,
    IN p_project_id INT,
    IN p_assigned_to INT
)
BEGIN
    SELECT t.id, t.title, t.description, t.due_date, t.task_type, t.progress,
           c.name as category_name, p.name as priority_name, s.name as status_name,
           CONCAT(u.first_name, ' ', u.last_name) as assignee_name,
           proj.name as project_name
    FROM tasks t
    LEFT JOIN categories c ON t.category_id = c.id
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN users u ON t.assigned_to = u.id
    LEFT JOIN projects proj ON t.project_id = proj.id
    WHERE (p_status_id IS NULL OR t.status_id = p_status_id)
      AND (p_priority_id IS NULL OR t.priority_id = p_priority_id)
      AND (p_project_id IS NULL OR t.project_id = p_project_id)
      AND (p_assigned_to IS NULL OR t.assigned_to = p_assigned_to)
    ORDER BY t.created_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskAssignmentForm;$
CREATE PROCEDURE sp_GetTaskAssignmentForm(
    IN p_task_id INT
)
BEGIN
    SELECT id, title, description, project_id, category_id, priority_id, status_id, 
           task_type, assigned_to, start_date, due_date, estimated_hours, is_billable, hourly_rate, tags
    FROM tasks WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_AssignTask;$
CREATE PROCEDURE sp_AssignTask(
    IN p_title VARCHAR(255),
    IN p_description TEXT,
    IN p_project_id INT,
    IN p_category_id INT,
    IN p_priority_id INT,
    IN p_status_id INT,
    IN p_task_type VARCHAR(50),
    IN p_assigned_to INT,
    IN p_assigned_by INT,
    IN p_assigned_at DATETIME,
    IN p_start_date DATE,
    IN p_due_date DATE,
    IN p_estimated_hours DECIMAL(5,2),
    IN p_is_billable BOOLEAN,
    IN p_hourly_rate DECIMAL(10,2),
    IN p_tags TEXT,
    IN p_created_by INT,
    IN p_created_at DATETIME,
    IN p_updated_at DATETIME
)
BEGIN
    INSERT INTO tasks (title, description, project_id, category_id, priority_id, status_id, 
                      task_type, assigned_to, assigned_by, assigned_at, start_date, due_date, 
                      estimated_hours, is_billable, hourly_rate, tags, created_by, created_at, updated_at)
    VALUES (p_title, p_description, p_project_id, p_category_id, p_priority_id, p_status_id,
            p_task_type, p_assigned_to, p_assigned_by, p_assigned_at, p_start_date, p_due_date,
            p_estimated_hours, p_is_billable, p_hourly_rate, p_tags, p_created_by, p_created_at, p_updated_at);
    
    SELECT LAST_INSERT_ID() as id;
END$$

DROP PROCEDURE IF EXISTS sp_GetOldAssignee;$
CREATE PROCEDURE sp_GetOldAssignee(
    IN p_task_id INT
)
BEGIN
    SELECT assigned_to FROM tasks WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateTaskAssignment;$
CREATE PROCEDURE sp_UpdateTaskAssignment(
    IN p_task_id INT,
    IN p_title VARCHAR(255),
    IN p_description TEXT,
    IN p_project_id INT,
    IN p_category_id INT,
    IN p_priority_id INT,
    IN p_status_id INT,
    IN p_task_type VARCHAR(50),
    IN p_assigned_to INT,
    IN p_start_date DATE,
    IN p_due_date DATE,
    IN p_estimated_hours DECIMAL(5,2),
    IN p_is_billable BOOLEAN,
    IN p_hourly_rate DECIMAL(10,2),
    IN p_tags TEXT,
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE tasks SET 
        title = p_title, 
        description = p_description, 
        project_id = p_project_id,
        category_id = p_category_id, 
        priority_id = p_priority_id, 
        status_id = p_status_id,
        task_type = p_task_type, 
        assigned_to = p_assigned_to, 
        start_date = p_start_date,
        due_date = p_due_date, 
        estimated_hours = p_estimated_hours, 
        is_billable = p_is_billable,
        hourly_rate = p_hourly_rate, 
        tags = p_tags, 
        updated_at = p_updated_at
    WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetEmployeeTasks;$
CREATE PROCEDURE sp_GetEmployeeTasks(
    IN p_user_id INT,
    IN p_status_id INT,
    IN p_priority_id INT,
    IN p_sort_by VARCHAR(50)
)
BEGIN
    DECLARE sort_clause VARCHAR(100);
    
    SET sort_clause = CASE p_sort_by
        WHEN 'due_date' THEN 't.due_date ASC'
        WHEN 'priority' THEN 'p.level DESC'
        WHEN 'status' THEN 's.sort_order'
        ELSE 't.created_at DESC'
    END;
    
    SET @sql = CONCAT('
        SELECT t.id, t.title, t.description, t.due_date, t.task_type, t.progress, t.status_id,
               c.name as category_name, p.name as priority_name, s.name as status_name,
               proj.name as project_name
        FROM tasks t
        LEFT JOIN categories c ON t.category_id = c.id
        LEFT JOIN priorities p ON t.priority_id = p.id
        LEFT JOIN statuses s ON t.status_id = s.id
        LEFT JOIN projects proj ON t.project_id = proj.id
        WHERE t.assigned_to = ', p_user_id);
    
    IF p_status_id IS NOT NULL THEN
        SET @sql = CONCAT(@sql, ' AND t.status_id = ', p_status_id);
    END IF;
    
    IF p_priority_id IS NOT NULL THEN
        SET @sql = CONCAT(@sql, ' AND t.priority_id = ', p_priority_id);
    END IF;
    
    SET @sql = CONCAT(@sql, ' ORDER BY ', sort_clause);
    
    PREPARE stmt FROM @sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskDetailsEnhanced;$
CREATE PROCEDURE sp_GetTaskDetailsEnhanced(
    IN p_task_id INT
)
BEGIN
    SELECT t.id, t.title, t.description, t.due_date, t.start_date, t.completed_date, 
           t.task_type, t.progress, t.estimated_hours, t.actual_hours, t.is_billable, t.tags,
           t.created_at, t.updated_at, t.project_id, t.assigned_to, t.assigned_at, t.created_by,
           c.name as category_name, p.name as priority_name, s.name as status_name,
           proj.name as project_name, proj.code as project_code,
           CONCAT(u.first_name, ' ', u.last_name) as assignee_name, u.profile_image as assignee_image,
           CONCAT(ab.first_name, ' ', ab.last_name) as assigned_by_name,
           CONCAT(cr.first_name, ' ', cr.last_name) as creator_name
    FROM tasks t
    LEFT JOIN categories c ON t.category_id = c.id
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN projects proj ON t.project_id = proj.id
    LEFT JOIN users u ON t.assigned_to = u.id
    LEFT JOIN users ab ON t.assigned_by = ab.id
    LEFT JOIN users cr ON t.created_by = cr.id
    WHERE t.id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateTaskStatusWithCompletion;$
CREATE PROCEDURE sp_UpdateTaskStatusWithCompletion(
    IN p_task_id INT,
    IN p_status_id INT,
    IN p_completed_date DATETIME,
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE tasks 
    SET status_id = p_status_id, 
        completed_date = p_completed_date, 
        updated_at = p_updated_at 
    WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetUserTimeLogs;$
CREATE PROCEDURE sp_GetUserTimeLogs(
    IN p_user_id INT,
    IN p_start_date DATE,
    IN p_end_date DATE
)
BEGIN
    SELECT tl.id, tl.task_id, tl.hours_logged, tl.log_date, tl.description, tl.is_billable, tl.is_approved,
           t.title as task_title
    FROM time_logs tl
    JOIN tasks t ON tl.task_id = t.id
    WHERE tl.user_id = p_user_id AND tl.log_date BETWEEN p_start_date AND p_end_date
    ORDER BY tl.log_date DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetProjects;$
CREATE PROCEDURE sp_GetProjects()
BEGIN
    SELECT id, name FROM projects WHERE is_active = 1 ORDER BY name;
END$$

DROP PROCEDURE IF EXISTS sp_GetEmployees;$
CREATE PROCEDURE sp_GetEmployees(
    IN p_exclude_user_id INT
)
BEGIN
    IF p_exclude_user_id IS NULL THEN
        SELECT id, CONCAT(first_name, ' ', last_name) as full_name 
        FROM users WHERE is_active = 1
        ORDER BY first_name;
    ELSE
        SELECT id, CONCAT(first_name, ' ', last_name) as full_name 
        FROM users WHERE is_active = 1 AND id != p_exclude_user_id
        ORDER BY first_name;
    END IF;
END$$

DELIMITER ;

-- Final success message
SELECT 'ALL STORED PROCEDURES CREATED SUCCESSFULLY!' as Status;
SELECT 'Total procedures created: 100+' as Info;

 
 - -   = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = =  
 - -   B L O B   S T O R A G E   P R O C E D U R E S   ( F i l e s   &   I m a g e s )  
 - -   = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = =  
  
 D R O P   P R O C E D U R E   I F   E X I S T S   s p _ D o w n l o a d A t t a c h m e n t ; $ $  
 C R E A T E   P R O C E D U R E   s p _ D o w n l o a d A t t a c h m e n t (  
         I N   p _ i d   I N T  
 )  
 B E G I N  
         S E L E C T   f i l e _ c o n t e n t ,   c o n t e n t _ t y p e ,   f i l e _ n a m e ,   o r i g i n a l _ n a m e  
         F R O M   t a s k _ a t t a c h m e n t s  
         W H E R E   i d   =   p _ i d ;  
 E N D $ $  
  
 D R O P   P R O C E D U R E   I F   E X I S T S   s p _ S a v e P r o f i l e I m a g e ; $ $  
 C R E A T E   P R O C E D U R E   s p _ S a v e P r o f i l e I m a g e (  
         I N   p _ u s e r _ i d   I N T ,  
         I N   p _ p r o f i l e _ i m a g e _ c o n t e n t   L O N G B L O B ,  
         I N   p _ p r o f i l e _ i m a g e _ t y p e   V A R C H A R ( 1 0 0 ) ,  
         I N   p _ p r o f i l e _ i m a g e   V A R C H A R ( 5 0 0 )  
 )  
 B E G I N  
         U P D A T E   u s e r s    
         S E T   p r o f i l e _ i m a g e _ c o n t e n t   =   p _ p r o f i l e _ i m a g e _ c o n t e n t ,  
                 p r o f i l e _ i m a g e _ t y p e   =   p _ p r o f i l e _ i m a g e _ t y p e ,  
                 p r o f i l e _ i m a g e   =   p _ p r o f i l e _ i m a g e  
         W H E R E   i d   =   p _ u s e r _ i d ;  
 E N D $ $  
  
 D R O P   P R O C E D U R E   I F   E X I S T S   s p _ G e t P r o f i l e I m a g e C o n t e n t ; $ $  
 C R E A T E   P R O C E D U R E   s p _ G e t P r o f i l e I m a g e C o n t e n t (  
         I N   p _ u s e r _ i d   I N T  
 )  
 B E G I N  
         S E L E C T   p r o f i l e _ i m a g e _ c o n t e n t ,   p r o f i l e _ i m a g e _ t y p e  
         F R O M   u s e r s  
         W H E R E   i d   =   p _ u s e r _ i d ;  
 E N D $ $  
  
 D R O P   P R O C E D U R E   I F   E X I S T S   s p _ R e m o v e P r o f i l e I m a g e B l o b ; $ $  
 C R E A T E   P R O C E D U R E   s p _ R e m o v e P r o f i l e I m a g e B l o b (  
         I N   p _ u s e r _ i d   I N T  
 )  
 B E G I N  
         U P D A T E   u s e r s    
         S E T   p r o f i l e _ i m a g e _ c o n t e n t   =   N U L L ,  
                 p r o f i l e _ i m a g e _ t y p e   =   N U L L ,  
                 p r o f i l e _ i m a g e   =   N U L L  
         W H E R E   i d   =   p _ u s e r _ i d ;  
 E N D $ $  
 
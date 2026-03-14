-- ============================================
-- REMAINING SERVICES STORED PROCEDURES
-- ProjectService, NotificationService, ManagerService procedures
-- ============================================

USE task_manager_db;

DELIMITER $$

-- ============================================
-- PROJECT SERVICE PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_GetProjectsWithFilters$$
CREATE PROCEDURE sp_GetProjectsWithFilters(
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

DROP PROCEDURE IF EXISTS sp_GetProjectForEdit$$
CREATE PROCEDURE sp_GetProjectForEdit(
    IN p_id INT
)
BEGIN
    SELECT id, name, description, code, department_id, manager_id, 
           start_date, end_date, budget, status, priority
    FROM projects WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetProjectDetails$$
CREATE PROCEDURE sp_GetProjectDetails(
    IN p_id INT
)
BEGIN
    SELECT p.id, p.name, p.description, p.code, p.start_date, p.end_date, p.budget, p.status, p.priority, p.created_at,
           p.manager_id, d.name as department_name, CONCAT(m.first_name, ' ', m.last_name) as manager_name
    FROM projects p
    LEFT JOIN departments d ON p.department_id = d.id
    LEFT JOIN users m ON p.manager_id = m.id
    WHERE p.id = p_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetProjectTaskStats$$
CREATE PROCEDURE sp_GetProjectTaskStats(
    IN p_project_id INT
)
BEGIN
    SELECT 
        COUNT(*) as total,
        SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) as completed,
        SUM(CASE WHEN s.name IN ('in_progress', 'in_review', 'testing') THEN 1 ELSE 0 END) as in_progress,
        SUM(CASE WHEN due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled') THEN 1 ELSE 0 END) as overdue
    FROM tasks t
    JOIN statuses s ON t.status_id = s.id
    WHERE t.project_id = p_project_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetProjectTotalHours$$
CREATE PROCEDURE sp_GetProjectTotalHours(
    IN p_project_id INT
)
BEGIN
    SELECT COALESCE(SUM(hours_logged), 0) as total_hours 
    FROM time_logs 
    WHERE project_id = p_project_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetProjectTasks$$
CREATE PROCEDURE sp_GetProjectTasks(
    IN p_project_id INT
)
BEGIN
    SELECT t.id, t.title, t.description, t.due_date,
           p.name as priority_name, s.name as status_name, c.name as category_name
    FROM tasks t
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN categories c ON t.category_id = c.id
    WHERE t.project_id = p_project_id
    ORDER BY t.due_date;
END$$

DROP PROCEDURE IF EXISTS sp_CreateProject$$
CREATE PROCEDURE sp_CreateProject(
    IN p_name VARCHAR(200),
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

DROP PROCEDURE IF EXISTS sp_CheckProjectCodeExists$$
CREATE PROCEDURE sp_CheckProjectCodeExists(
    IN p_code VARCHAR(50),
    IN p_exclude_id INT
)
BEGIN
    SELECT COUNT(*) as count 
    FROM projects 
    WHERE code = p_code 
      AND (p_exclude_id IS NULL OR id != p_exclude_id);
END$$

DROP PROCEDURE IF EXISTS sp_GetNextProjectCode$$
CREATE PROCEDURE sp_GetNextProjectCode()
BEGIN
    SELECT code FROM projects 
    WHERE code LIKE 'PRJ%' 
    ORDER BY CAST(SUBSTRING(code, 4) AS UNSIGNED) DESC 
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateProject$$
CREATE PROCEDURE sp_UpdateProject(
    IN p_id INT,
    IN p_name VARCHAR(200),
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
        name = p_name, description = p_description, code = p_code,
        department_id = p_department_id, manager_id = p_manager_id,
        start_date = p_start_date, end_date = p_end_date,
        budget = p_budget, status = p_status, priority = p_priority,
        updated_at = p_updated_at
    WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS sp_DeleteProject$$
CREATE PROCEDURE sp_DeleteProject(
    IN p_id INT
)
BEGIN
    -- Unassign all tasks from this project
    UPDATE tasks SET project_id = NULL WHERE project_id = p_id;
    
    -- Soft delete: Set project as inactive
    UPDATE projects SET is_active = 0 WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetProjectsForSelect$$
CREATE PROCEDURE sp_GetProjectsForSelect()
BEGIN
    SELECT id, name FROM projects 
    WHERE is_active = 1 AND status != 'completed' 
    ORDER BY name;
END$$

DROP PROCEDURE IF EXISTS sp_GetDepartments$$
CREATE PROCEDURE sp_GetDepartments()
BEGIN
    SELECT id, name FROM departments WHERE is_active = 1 ORDER BY name;
END$$

DROP PROCEDURE IF EXISTS sp_GetProjectManagers$$
CREATE PROCEDURE sp_GetProjectManagers()
BEGIN
    SELECT id, CONCAT(first_name, ' ', last_name) as full_name 
    FROM users WHERE is_active = 1 AND role_id IN (1, 2) ORDER BY first_name;
END$$

-- ============================================
-- NOTIFICATION SERVICE PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_GetUserNotifications$$
CREATE PROCEDURE sp_GetUserNotifications(
    IN p_user_id INT,
    IN p_limit INT,
    IN p_unread_only BOOLEAN
)
BEGIN
    SELECT id, title, message, type, reference_type, reference_id, is_read, created_at
    FROM notifications
    WHERE user_id = p_user_id
      AND (p_unread_only = 0 OR is_read = 0)
    ORDER BY created_at DESC 
    LIMIT p_limit;
END$$

DROP PROCEDURE IF EXISTS sp_GetUnreadNotificationCount$$
CREATE PROCEDURE sp_GetUnreadNotificationCount(
    IN p_user_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM notifications 
    WHERE user_id = p_user_id AND is_read = 0;
END$$

DROP PROCEDURE IF EXISTS sp_MarkNotificationAsRead$$
CREATE PROCEDURE sp_MarkNotificationAsRead(
    IN p_id INT,
    IN p_user_id INT,
    IN p_read_at DATETIME
)
BEGIN
    UPDATE notifications 
    SET is_read = 1, read_at = p_read_at 
    WHERE id = p_id AND user_id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_MarkAllNotificationsAsRead$$
CREATE PROCEDURE sp_MarkAllNotificationsAsRead(
    IN p_user_id INT,
    IN p_read_at DATETIME
)
BEGIN
    UPDATE notifications 
    SET is_read = 1, read_at = p_read_at 
    WHERE user_id = p_user_id AND is_read = 0;
END$$

DROP PROCEDURE IF EXISTS sp_CreateNotification$$
CREATE PROCEDURE sp_CreateNotification(
    IN p_user_id INT,
    IN p_title VARCHAR(200),
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

DROP PROCEDURE IF EXISTS sp_GetAdminUserIds$$
CREATE PROCEDURE sp_GetAdminUserIds()
BEGIN
    SELECT id FROM users WHERE role_id = 1 AND is_active = 1;
END$$

DROP PROCEDURE IF EXISTS sp_CleanupOldNotifications$$
CREATE PROCEDURE sp_CleanupOldNotifications(
    IN p_days_old INT
)
BEGIN
    DELETE FROM notifications 
    WHERE created_at < DATE_SUB(NOW(), INTERVAL p_days_old DAY) 
      AND is_read = 1;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskComments$$
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

DROP PROCEDURE IF EXISTS sp_GetCommentReplies$$
CREATE PROCEDURE sp_GetCommentReplies(
    IN p_parent_id INT
)
BEGIN
    SELECT c.id, c.task_id, c.user_id, c.comment, c.comment_type, c.is_internal, c.created_at,
           CONCAT(u.first_name, ' ', u.last_name) as user_name, u.profile_image
    FROM task_comments c
    JOIN users u ON c.user_id = u.id
    WHERE c.parent_comment_id = p_parent_id
    ORDER BY c.created_at;
END$$

DROP PROCEDURE IF EXISTS sp_AddComment$$
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
END$$

DROP PROCEDURE IF EXISTS sp_UpdateTaskCommentCount$$
CREATE PROCEDURE sp_UpdateTaskCommentCount(
    IN p_task_id INT
)
BEGIN
    UPDATE tasks SET comments_count = comments_count + 1 WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskProjectId$$
CREATE PROCEDURE sp_GetTaskProjectId(
    IN p_task_id INT
)
BEGIN
    SELECT project_id FROM tasks WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_LogTime$$
CREATE PROCEDURE sp_LogTime(
    IN p_task_id INT,
    IN p_user_id INT,
    IN p_project_id INT,
    IN p_hours DECIMAL(10,2),
    IN p_log_date DATE,
    IN p_description TEXT,
    IN p_is_billable BOOLEAN,
    IN p_created_at DATETIME
)
BEGIN
    INSERT INTO time_logs (task_id, user_id, project_id, hours_logged, log_date, description, is_billable, created_at)
    VALUES (p_task_id, p_user_id, p_project_id, p_hours, p_log_date, p_description, p_is_billable, p_created_at);
    
    SELECT LAST_INSERT_ID() as id;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateTaskActualHours$$
CREATE PROCEDURE sp_UpdateTaskActualHours(
    IN p_task_id INT,
    IN p_hours DECIMAL(10,2),
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE tasks SET actual_hours = actual_hours + p_hours, updated_at = p_updated_at 
    WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskTimeLogs$$
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

DROP PROCEDURE IF EXISTS sp_LogTaskActivity$$
CREATE PROCEDURE sp_LogTaskActivity(
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

DROP PROCEDURE IF EXISTS sp_GetTaskActivityLog$$
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

DROP PROCEDURE IF EXISTS sp_GetRecentActivity$$
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

SELECT 'Remaining services stored procedures created successfully!' as Status;

-- ============================================
-- FINAL THREE SERVICES STORED PROCEDURES
-- NotificationService, ManagerService, UserService
-- ============================================

DELIMITER $$

-- ============================================
-- NOTIFICATION SERVICE PROCEDURES
-- ============================================

-- Get user notifications
DROP PROCEDURE IF EXISTS sp_GetUserNotifications$$
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
        ORDER BY created_at DESC
        LIMIT p_limit;
    ELSE
        SELECT id, title, message, type, reference_type, reference_id, is_read, created_at
        FROM notifications
        WHERE user_id = p_user_id
        ORDER BY created_at DESC
        LIMIT p_limit;
    END IF;
END$$

-- Get unread notification count
DROP PROCEDURE IF EXISTS sp_GetUnreadNotificationCount$$
CREATE PROCEDURE sp_GetUnreadNotificationCount(IN p_user_id INT)
BEGIN
    SELECT COUNT(*) as count FROM notifications 
    WHERE user_id = p_user_id AND is_read = 0;
END$$

-- Mark notification as read
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

-- Mark all notifications as read
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


-- Create notification
DROP PROCEDURE IF EXISTS sp_CreateNotification$$
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

-- Get admin user IDs
DROP PROCEDURE IF EXISTS sp_GetAdminUserIds$$
CREATE PROCEDURE sp_GetAdminUserIds()
BEGIN
    SELECT id FROM users WHERE role_id = 1 AND is_active = 1;
END$$

-- Cleanup old notifications
DROP PROCEDURE IF EXISTS sp_CleanupOldNotifications$$
CREATE PROCEDURE sp_CleanupOldNotifications(IN p_days INT)
BEGIN
    DELETE FROM notifications 
    WHERE created_at < DATE_SUB(NOW(), INTERVAL p_days DAY) AND is_read = 1;
END$$

-- ============================================
-- COMMENT SERVICE PROCEDURES
-- ============================================

-- Get task comments
DROP PROCEDURE IF EXISTS sp_GetTaskComments$$
CREATE PROCEDURE sp_GetTaskComments(IN p_task_id INT)
BEGIN
    SELECT c.id, c.task_id, c.user_id, c.comment, c.comment_type, c.is_internal, 
           c.parent_comment_id, c.created_at,
           CONCAT(u.first_name, ' ', u.last_name) as user_name, u.profile_image
    FROM task_comments c
    JOIN users u ON c.user_id = u.id
    WHERE c.task_id = p_task_id AND c.parent_comment_id IS NULL
    ORDER BY c.created_at DESC;
END$$

-- Get comment replies
DROP PROCEDURE IF EXISTS sp_GetCommentReplies$$
CREATE PROCEDURE sp_GetCommentReplies(IN p_parent_id INT)
BEGIN
    SELECT c.id, c.task_id, c.user_id, c.comment, c.comment_type, c.is_internal, c.created_at,
           CONCAT(u.first_name, ' ', u.last_name) as user_name, u.profile_image
    FROM task_comments c
    JOIN users u ON c.user_id = u.id
    WHERE c.parent_comment_id = p_parent_id
    ORDER BY c.created_at;
END$$


-- Add comment
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
    
    UPDATE tasks SET comments_count = comments_count + 1 WHERE id = p_task_id;
END$$

-- Get task project ID
DROP PROCEDURE IF EXISTS sp_GetTaskProjectId$$
CREATE PROCEDURE sp_GetTaskProjectId(IN p_task_id INT)
BEGIN
    SELECT project_id FROM tasks WHERE id = p_task_id;
END$$

-- Log time
DROP PROCEDURE IF EXISTS sp_LogTime$$
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

-- Get task time logs
DROP PROCEDURE IF EXISTS sp_GetTaskTimeLogs$$
CREATE PROCEDURE sp_GetTaskTimeLogs(IN p_task_id INT)
BEGIN
    SELECT tl.id, tl.task_id, tl.user_id, tl.hours_logged, tl.log_date, tl.description, 
           tl.is_billable, tl.is_approved,
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

-- Log activity
DROP PROCEDURE IF EXISTS sp_LogActivity$$
CREATE PROCEDURE sp_LogActivity(
    IN p_task_id INT,
    IN p_user_id INT,
    IN p_action VARCHAR(100),
    IN p_field_changed VARCHAR(100),
    IN p_old_value TEXT,
    IN p_new_value TEXT,
    IN p_ip_address VARCHAR(45),
    IN p_created_at DATETIME
)
BEGIN
    INSERT INTO task_activity_log (task_id, user_id, action, field_changed, old_value, new_value, ip_address, created_at)
    VALUES (p_task_id, p_user_id, p_action, p_field_changed, p_old_value, p_new_value, p_ip_address, p_created_at);
END$$


-- Get task activity log
DROP PROCEDURE IF EXISTS sp_GetTaskActivityLog$$
CREATE PROCEDURE sp_GetTaskActivityLog(IN p_task_id INT)
BEGIN
    SELECT al.id, al.task_id, al.action, al.field_changed, al.old_value, al.new_value, al.created_at,
           CONCAT(u.first_name, ' ', u.last_name) as user_name, u.profile_image
    FROM task_activity_log al
    JOIN users u ON al.user_id = u.id
    WHERE al.task_id = p_task_id
    ORDER BY al.created_at DESC;
END$$

-- Get recent activity
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

-- ============================================
-- MANAGER SERVICE PROCEDURES
-- ============================================

-- Get manager department
DROP PROCEDURE IF EXISTS sp_GetManagerDepartment$$
CREATE PROCEDURE sp_GetManagerDepartment(IN p_manager_id INT)
BEGIN
    SELECT department_id FROM users WHERE id = p_manager_id;
END$$

-- Get department name
DROP PROCEDURE IF EXISTS sp_GetDepartmentName$$
CREATE PROCEDURE sp_GetDepartmentName(IN p_dept_id INT)
BEGIN
    SELECT name FROM departments WHERE id = p_dept_id;
END$$

-- Get team member count
DROP PROCEDURE IF EXISTS sp_GetTeamMemberCount$$
CREATE PROCEDURE sp_GetTeamMemberCount(IN p_dept_id INT)
BEGIN
    SELECT COUNT(*) as count FROM users WHERE department_id = p_dept_id AND is_active = 1;
END$$

-- Get active projects count for manager
DROP PROCEDURE IF EXISTS sp_GetActiveProjectsCountByManager$$
CREATE PROCEDURE sp_GetActiveProjectsCountByManager(IN p_manager_id INT)
BEGIN
    SELECT COUNT(*) as count FROM projects 
    WHERE manager_id = p_manager_id AND is_active = 1 AND status IN ('planning', 'active');
END$$

-- Get projects on track count
DROP PROCEDURE IF EXISTS sp_GetProjectsOnTrackCount$$
CREATE PROCEDURE sp_GetProjectsOnTrackCount(IN p_manager_id INT)
BEGIN
    SELECT COUNT(*) as count FROM projects 
    WHERE manager_id = p_manager_id 
    AND is_active = 1 
    AND status = 'active'
    AND (end_date IS NULL OR end_date >= CURDATE());
END$$


-- Get projects at risk count
DROP PROCEDURE IF EXISTS sp_GetProjectsAtRiskCount$$
CREATE PROCEDURE sp_GetProjectsAtRiskCount(IN p_manager_id INT)
BEGIN
    SELECT COUNT(*) as count FROM projects 
    WHERE manager_id = p_manager_id 
    AND is_active = 1 
    AND status = 'active'
    AND end_date IS NOT NULL
    AND DATEDIFF(end_date, CURDATE()) BETWEEN 1 AND 7;
END$$

-- Get projects delayed count
DROP PROCEDURE IF EXISTS sp_GetProjectsDelayedCount$$
CREATE PROCEDURE sp_GetProjectsDelayedCount(IN p_manager_id INT)
BEGIN
    SELECT COUNT(*) as count FROM projects 
    WHERE manager_id = p_manager_id 
    AND is_active = 1 
    AND status = 'active'
    AND end_date < CURDATE();
END$$

-- Get total tasks count
DROP PROCEDURE IF EXISTS sp_GetManagerTotalTasksCount$$
CREATE PROCEDURE sp_GetManagerTotalTasksCount(IN p_manager_id INT)
BEGIN
    SELECT COUNT(*) as count FROM tasks t
    INNER JOIN projects p ON t.project_id = p.id
    WHERE p.manager_id = p_manager_id;
END$$

-- Get completed this week count
DROP PROCEDURE IF EXISTS sp_GetCompletedThisWeekCount$$
CREATE PROCEDURE sp_GetCompletedThisWeekCount(IN p_manager_id INT)
BEGIN
    SELECT COUNT(*) as count FROM tasks t
    INNER JOIN projects p ON t.project_id = p.id
    WHERE p.manager_id = p_manager_id
    AND t.status_id = 4
    AND t.updated_at >= DATE_SUB(CURDATE(), INTERVAL 7 DAY);
END$$

-- Get overdue tasks count
DROP PROCEDURE IF EXISTS sp_GetManagerOverdueTasksCount$$
CREATE PROCEDURE sp_GetManagerOverdueTasksCount(IN p_manager_id INT)
BEGIN
    SELECT COUNT(*) as count FROM tasks t
    INNER JOIN projects p ON t.project_id = p.id
    WHERE p.manager_id = p_manager_id
    AND t.status_id != 4
    AND t.due_date < CURDATE();
END$$

-- Get in progress tasks count
DROP PROCEDURE IF EXISTS sp_GetManagerInProgressTasksCount$$
CREATE PROCEDURE sp_GetManagerInProgressTasksCount(IN p_manager_id INT)
BEGIN
    SELECT COUNT(*) as count FROM tasks t
    INNER JOIN projects p ON t.project_id = p.id
    WHERE p.manager_id = p_manager_id
    AND t.status_id IN (2, 3);
END$$

-- Get team completion rate
DROP PROCEDURE IF EXISTS sp_GetTeamCompletionRate$$
CREATE PROCEDURE sp_GetTeamCompletionRate(IN p_dept_id INT)
BEGIN
    SELECT COALESCE(
        (SUM(CASE WHEN t.status_id = 4 THEN 1 ELSE 0 END) * 100.0 / COUNT(*)), 
        0
    ) as completion_rate
    FROM tasks t
    INNER JOIN users u ON t.assigned_to = u.id
    WHERE u.department_id = p_dept_id
    AND t.created_at >= DATE_SUB(CURDATE(), INTERVAL 30 DAY);
END$$


-- Get average task time
DROP PROCEDURE IF EXISTS sp_GetAverageTaskTime$$
CREATE PROCEDURE sp_GetAverageTaskTime(IN p_dept_id INT)
BEGIN
    SELECT COALESCE(AVG(DATEDIFF(t.updated_at, t.created_at)), 0) as avg_days
    FROM tasks t
    INNER JOIN users u ON t.assigned_to = u.id
    WHERE u.department_id = p_dept_id
    AND t.status_id = 4
    AND t.updated_at >= DATE_SUB(CURDATE(), INTERVAL 30 DAY);
END$$

-- Get team members
DROP PROCEDURE IF EXISTS sp_GetTeamMembers$$
CREATE PROCEDURE sp_GetTeamMembers(
    IN p_dept_id INT,
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
    WHERE u.department_id = p_dept_id 
    AND u.is_active = 1
    AND u.id != p_manager_id
    ORDER BY u.first_name, u.last_name;
END$$

-- Get managed projects
DROP PROCEDURE IF EXISTS sp_GetManagedProjects$$
CREATE PROCEDURE sp_GetManagedProjects(
    IN p_manager_id INT,
    IN p_status VARCHAR(50)
)
BEGIN
    IF p_status IS NULL OR p_status = '' THEN
        SELECT p.id, p.name, p.code, p.description, p.status, p.priority,
               p.start_date, p.end_date, p.budget,
               d.name as department_name,
               (SELECT COUNT(*) FROM tasks WHERE project_id = p.id) as total_tasks,
               (SELECT COUNT(*) FROM tasks WHERE project_id = p.id AND status_id = 4) as completed_tasks
        FROM projects p
        LEFT JOIN departments d ON p.department_id = d.id
        WHERE p.manager_id = p_manager_id
        AND p.is_active = 1
        ORDER BY p.created_at DESC;
    ELSE
        SELECT p.id, p.name, p.code, p.description, p.status, p.priority,
               p.start_date, p.end_date, p.budget,
               d.name as department_name,
               (SELECT COUNT(*) FROM tasks WHERE project_id = p.id) as total_tasks,
               (SELECT COUNT(*) FROM tasks WHERE project_id = p.id AND status_id = 4) as completed_tasks
        FROM projects p
        LEFT JOIN departments d ON p.department_id = d.id
        WHERE p.manager_id = p_manager_id
        AND p.is_active = 1
        AND p.status = p_status
        ORDER BY p.created_at DESC;
    END IF;
END$$


-- Get team performance
DROP PROCEDURE IF EXISTS sp_GetTeamPerformance$$
CREATE PROCEDURE sp_GetTeamPerformance(
    IN p_dept_id INT,
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
    WHERE u.department_id = p_dept_id
    AND u.is_active = 1
    AND u.id != p_manager_id
    GROUP BY u.id, u.first_name, u.last_name, u.employee_id
    ORDER BY tasks_completed DESC;
END$$

-- Get recent activities for manager
DROP PROCEDURE IF EXISTS sp_GetManagerRecentActivities$$
CREATE PROCEDURE sp_GetManagerRecentActivities(
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

-- Get top performers
DROP PROCEDURE IF EXISTS sp_GetTopPerformers$$
CREATE PROCEDURE sp_GetTopPerformers(
    IN p_dept_id INT,
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
    WHERE u.department_id = p_dept_id
    AND u.is_active = 1
    GROUP BY u.id, u.first_name, u.last_name, u.employee_id
    HAVING total_tasks > 0
    ORDER BY completed_tasks DESC, on_time_tasks DESC
    LIMIT p_limit;
END$$

-- Get upcoming deadlines
DROP PROCEDURE IF EXISTS sp_GetUpcomingDeadlines$$
CREATE PROCEDURE sp_GetUpcomingDeadlines(
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


-- ============================================
-- USER SERVICE PROCEDURES
-- ============================================

-- Get users with filters
DROP PROCEDURE IF EXISTS sp_GetUsersWithFilters$$
CREATE PROCEDURE sp_GetUsersWithFilters(
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_team_id INT,
    IN p_is_active BOOLEAN
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.email, u.employee_id, u.job_title, 
           u.profile_image, u.is_active, u.last_login,
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

-- Get users grouped by team
DROP PROCEDURE IF EXISTS sp_GetUsersGroupedByTeam$$
CREATE PROCEDURE sp_GetUsersGroupedByTeam(
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_is_active BOOLEAN
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.email, u.employee_id, u.job_title, 
           u.profile_image, u.is_active, u.last_login, u.team_id,
           r.name as role_name, d.name as department_name, t.name as team_name,
           CONCAT(m.first_name, ' ', m.last_name) as manager_name, m.id as manager_id,
           (SELECT COUNT(*) FROM tasks WHERE assigned_to = u.id) as task_count
    FROM users u
    LEFT JOIN roles r ON u.role_id = r.id
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    LEFT JOIN users m ON t.team_lead_id = m.id OR d.manager_id = m.id
    WHERE (p_role_id IS NULL OR u.role_id = p_role_id)
    AND (p_department_id IS NULL OR u.department_id = p_department_id)
    AND (p_is_active IS NULL OR u.is_active = p_is_active)
    ORDER BY t.name, d.name, u.first_name, u.last_name;
END$$

-- Get user for edit
DROP PROCEDURE IF EXISTS sp_GetUserForEdit$$
CREATE PROCEDURE sp_GetUserForEdit(IN p_id INT)
BEGIN
    SELECT id, first_name, last_name, email, phone, employee_id, job_title, 
           role_id, department_id, team_id, reports_to, hire_date, is_active
    FROM users WHERE id = p_id;
END$$

-- Generate employee ID
DROP PROCEDURE IF EXISTS sp_GenerateEmployeeId$$
CREATE PROCEDURE sp_GenerateEmployeeId()
BEGIN
    SELECT employee_id FROM users 
    WHERE employee_id LIKE 'EMP%' 
    ORDER BY CAST(SUBSTRING(employee_id, 4) AS UNSIGNED) DESC 
    LIMIT 1;
END$$

-- Create user
DROP PROCEDURE IF EXISTS sp_CreateUser$$
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


-- Update user
DROP PROCEDURE IF EXISTS sp_UpdateUser$$
CREATE PROCEDURE sp_UpdateUser(
    IN p_id INT,
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
    WHERE id = p_id;
END$$

-- Update user with password
DROP PROCEDURE IF EXISTS sp_UpdateUserWithPassword$$
CREATE PROCEDURE sp_UpdateUserWithPassword(
    IN p_id INT,
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
    WHERE id = p_id;
END$$

-- Check if user is admin
DROP PROCEDURE IF EXISTS sp_CheckUserIsAdmin$$
CREATE PROCEDURE sp_CheckUserIsAdmin(IN p_id INT)
BEGIN
    SELECT email FROM users WHERE id = p_id;
END$$

-- Unassign user tasks
DROP PROCEDURE IF EXISTS sp_UnassignUserTasks$$
CREATE PROCEDURE sp_UnassignUserTasks(IN p_user_id INT)
BEGIN
    UPDATE tasks SET assigned_to = NULL WHERE assigned_to = p_user_id;
END$$

-- Soft delete user
DROP PROCEDURE IF EXISTS sp_SoftDeleteUser$$
CREATE PROCEDURE sp_SoftDeleteUser(IN p_id INT)
BEGIN
    UPDATE users SET is_active = 0 WHERE id = p_id;
END$$

-- Get employees for assignment
DROP PROCEDURE IF EXISTS sp_GetEmployeesForAssignment$$
CREATE PROCEDURE sp_GetEmployeesForAssignment()
BEGIN
    SELECT id, CONCAT(first_name, ' ', last_name) as full_name, job_title
    FROM users 
    WHERE is_active = 1 AND role_id != 1 
    ORDER BY first_name, last_name;
END$$


-- Get employee summaries
DROP PROCEDURE IF EXISTS sp_GetEmployeeSummaries$$
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

-- Get roles (excluding admin)
DROP PROCEDURE IF EXISTS sp_GetRolesExcludingAdmin$$
CREATE PROCEDURE sp_GetRolesExcludingAdmin()
BEGIN
    SELECT id, name FROM roles WHERE id != 1 ORDER BY id;
END$$

-- Get active departments
DROP PROCEDURE IF EXISTS sp_GetActiveDepartments$$
CREATE PROCEDURE sp_GetActiveDepartments()
BEGIN
    SELECT id, name FROM departments WHERE is_active = 1 ORDER BY name;
END$$

-- Get active teams
DROP PROCEDURE IF EXISTS sp_GetActiveTeams$$
CREATE PROCEDURE sp_GetActiveTeams()
BEGIN
    SELECT id, name FROM teams WHERE is_active = 1 ORDER BY name;
END$$

-- Get managers for dropdown
DROP PROCEDURE IF EXISTS sp_GetManagersForDropdown$$
CREATE PROCEDURE sp_GetManagersForDropdown()
BEGIN
    SELECT u.id, CONCAT(u.first_name, ' ', u.last_name) as full_name 
    FROM users u
    JOIN roles r ON u.role_id = r.id
    WHERE u.is_active = 1 AND (r.name = 'Admin' OR r.name = 'Manager')
    ORDER BY u.first_name;
END$$

-- Get user profile
DROP PROCEDURE IF EXISTS sp_GetUserProfile$$
CREATE PROCEDURE sp_GetUserProfile(IN p_user_id INT)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.email, u.phone, u.employee_id, u.job_title, 
           u.profile_image, u.hire_date,
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

-- Verify user password
DROP PROCEDURE IF EXISTS sp_VerifyUserPassword$$
CREATE PROCEDURE sp_VerifyUserPassword(IN p_id INT)
BEGIN
    SELECT password FROM users WHERE id = p_id AND is_active = 1;
END$$

-- Change user password
DROP PROCEDURE IF EXISTS sp_ChangeUserPassword$$
CREATE PROCEDURE sp_ChangeUserPassword(
    IN p_id INT,
    IN p_password VARCHAR(255)
)
BEGIN
    UPDATE users SET password = p_password WHERE id = p_id;
END$$


-- Get teams by department
DROP PROCEDURE IF EXISTS sp_GetTeamsByDepartment$$
CREATE PROCEDURE sp_GetTeamsByDepartment(IN p_department_id INT)
BEGIN
    SELECT id, name FROM teams 
    WHERE department_id = p_department_id AND is_active = 1 
    ORDER BY name;
END$$

-- Update profile picture
DROP PROCEDURE IF EXISTS sp_UpdateProfilePicture$$
CREATE PROCEDURE sp_UpdateProfilePicture(
    IN p_user_id INT,
    IN p_profile_image VARCHAR(255)
)
BEGIN
    UPDATE users SET profile_image = p_profile_image WHERE id = p_user_id;
END$$

-- Get current profile picture
DROP PROCEDURE IF EXISTS sp_GetCurrentProfilePicture$$
CREATE PROCEDURE sp_GetCurrentProfilePicture(IN p_user_id INT)
BEGIN
    SELECT profile_image FROM users WHERE id = p_user_id;
END$$

-- Remove profile picture
DROP PROCEDURE IF EXISTS sp_RemoveProfilePicture$$
CREATE PROCEDURE sp_RemoveProfilePicture(IN p_user_id INT)
BEGIN
    UPDATE users SET profile_image = NULL WHERE id = p_user_id;
END$$

-- Get profile for edit
DROP PROCEDURE IF EXISTS sp_GetProfileForEdit$$
CREATE PROCEDURE sp_GetProfileForEdit(IN p_user_id INT)
BEGIN
    SELECT id, first_name, last_name, email, phone, job_title, profile_image
    FROM users WHERE id = p_user_id;
END$$

-- Update profile
DROP PROCEDURE IF EXISTS sp_UpdateProfile$$
CREATE PROCEDURE sp_UpdateProfile(
    IN p_id INT,
    IN p_first_name VARCHAR(100),
    IN p_last_name VARCHAR(100),
    IN p_email VARCHAR(255),
    IN p_phone VARCHAR(20),
    IN p_job_title VARCHAR(100)
)
BEGIN
    UPDATE users 
    SET first_name = p_first_name,
        last_name = p_last_name,
        email = p_email,
        phone = p_phone,
        job_title = p_job_title
    WHERE id = p_id;
END$$

DELIMITER ;

-- ============================================
-- END OF STORED PROCEDURES
-- ============================================

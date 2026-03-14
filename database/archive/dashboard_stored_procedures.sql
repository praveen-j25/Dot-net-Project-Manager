-- ============================================
-- DASHBOARD SERVICE STORED PROCEDURES
-- Professional-grade stored procedures for dashboard operations
-- ============================================

USE task_manager_db;

DELIMITER $$

-- ============================================
-- ADMIN DASHBOARD PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_GetActiveEmployeeCount$$
CREATE PROCEDURE sp_GetActiveEmployeeCount()
BEGIN
    SELECT COUNT(*) as count FROM users WHERE is_active = 1 AND role_id = 3;
END$$

DROP PROCEDURE IF EXISTS sp_GetActiveProjectsCount$$
CREATE PROCEDURE sp_GetActiveProjectsCount()
BEGIN
    SELECT COUNT(*) as count FROM projects WHERE is_active = 1 AND status IN ('planning', 'active');
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskStatistics$$
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

DROP PROCEDURE IF EXISTS sp_GetTotalHoursLogged$$
CREATE PROCEDURE sp_GetTotalHoursLogged()
BEGIN
    SELECT COALESCE(SUM(hours_logged), 0) as total_hours FROM time_logs;
END$$

DROP PROCEDURE IF EXISTS sp_GetTasksByStatus$$
CREATE PROCEDURE sp_GetTasksByStatus()
BEGIN
    SELECT s.name, COUNT(t.id) as count
    FROM statuses s
    LEFT JOIN tasks t ON t.status_id = s.id
    GROUP BY s.id, s.name
    ORDER BY s.id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTasksByPriority$$
CREATE PROCEDURE sp_GetTasksByPriority()
BEGIN
    SELECT p.name, COUNT(t.id) as count
    FROM priorities p
    LEFT JOIN tasks t ON t.priority_id = p.id
    GROUP BY p.id, p.name
    ORDER BY p.id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTasksByDepartment$$
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

DROP PROCEDURE IF EXISTS sp_GetMonthlyTaskTrends$$
CREATE PROCEDURE sp_GetMonthlyTaskTrends()
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

DROP PROCEDURE IF EXISTS sp_GetTopPerformers$$
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

DROP PROCEDURE IF EXISTS sp_GetActiveProjectsList$$
CREATE PROCEDURE sp_GetActiveProjectsList()
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

DROP PROCEDURE IF EXISTS sp_GetRecentTasks$$
CREATE PROCEDURE sp_GetRecentTasks()
BEGIN
    SELECT t.id, t.title, t.due_date, p.name as priority_name, s.name as status_name
    FROM tasks t
    JOIN priorities p ON t.priority_id = p.id
    JOIN statuses s ON t.status_id = s.id
    ORDER BY t.created_at DESC
    LIMIT 5;
END$$

DROP PROCEDURE IF EXISTS sp_GetOverdueTasksList$$
CREATE PROCEDURE sp_GetOverdueTasksList()
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

-- ============================================
-- EMPLOYEE DASHBOARD PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_GetEmployeeDetails$$
CREATE PROCEDURE sp_GetEmployeeDetails(
    IN p_user_id INT
)
BEGIN
    SELECT u.job_title, d.name as department_name, t.name as team_name
    FROM users u
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    WHERE u.id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetEmployeeTaskStatistics$$
CREATE PROCEDURE sp_GetEmployeeTaskStatistics(
    IN p_user_id INT
)
BEGIN
    SELECT 
        COUNT(*) as total,
        SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) as completed,
        SUM(CASE WHEN s.name IN ('in_progress', 'in_review', 'testing') THEN 1 ELSE 0 END) as in_progress,
        SUM(CASE WHEN due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled') THEN 1 ELSE 0 END) as overdue
    FROM tasks t
    JOIN statuses s ON t.status_id = s.id
    WHERE t.assigned_to = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetEmployeeHoursThisWeek$$
CREATE PROCEDURE sp_GetEmployeeHoursThisWeek(
    IN p_user_id INT
)
BEGIN
    SELECT COALESCE(SUM(hours_logged), 0) as hours_this_week 
    FROM time_logs 
    WHERE user_id = p_user_id 
    AND log_date >= DATE_SUB(CURDATE(), INTERVAL WEEKDAY(CURDATE()) DAY);
END$$

DROP PROCEDURE IF EXISTS sp_GetEmployeeHoursThisMonth$$
CREATE PROCEDURE sp_GetEmployeeHoursThisMonth(
    IN p_user_id INT
)
BEGIN
    SELECT COALESCE(SUM(hours_logged), 0) as hours_this_month 
    FROM time_logs 
    WHERE user_id = p_user_id 
    AND MONTH(log_date) = MONTH(CURDATE()) 
    AND YEAR(log_date) = YEAR(CURDATE());
END$$

DROP PROCEDURE IF EXISTS sp_GetEmployeeTodaysTasks$$
CREATE PROCEDURE sp_GetEmployeeTodaysTasks(
    IN p_user_id INT
)
BEGIN
    SELECT t.id, t.title, t.due_date, p.name as priority_name, s.name as status_name
    FROM tasks t
    JOIN priorities p ON t.priority_id = p.id
    JOIN statuses s ON t.status_id = s.id
    WHERE t.assigned_to = p_user_id 
    AND t.due_date = CURDATE() 
    AND s.name NOT IN ('completed', 'cancelled')
    ORDER BY p.level DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetEmployeeUpcomingTasks$$
CREATE PROCEDURE sp_GetEmployeeUpcomingTasks(
    IN p_user_id INT
)
BEGIN
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
END$$

DROP PROCEDURE IF EXISTS sp_GetEmployeeOverdueTasks$$
CREATE PROCEDURE sp_GetEmployeeOverdueTasks(
    IN p_user_id INT
)
BEGIN
    SELECT t.id, t.title, t.due_date, p.name as priority_name, s.name as status_name
    FROM tasks t
    JOIN priorities p ON t.priority_id = p.id
    JOIN statuses s ON t.status_id = s.id
    WHERE t.assigned_to = p_user_id 
    AND t.due_date < CURDATE() 
    AND s.name NOT IN ('completed', 'cancelled')
    ORDER BY t.due_date ASC;
END$$

DROP PROCEDURE IF EXISTS sp_GetEmployeeRecentlyCompleted$$
CREATE PROCEDURE sp_GetEmployeeRecentlyCompleted(
    IN p_user_id INT
)
BEGIN
    SELECT t.id, t.title, t.completed_date as due_date, p.name as priority_name, s.name as status_name
    FROM tasks t
    JOIN priorities p ON t.priority_id = p.id
    JOIN statuses s ON t.status_id = s.id
    WHERE t.assigned_to = p_user_id 
    AND s.name = 'completed'
    ORDER BY t.completed_date DESC
    LIMIT 5;
END$$

DELIMITER ;

-- ============================================
-- VERIFICATION
-- ============================================
SELECT 'Dashboard stored procedures created successfully!' as Status;

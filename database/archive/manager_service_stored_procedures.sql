-- ============================================
-- MANAGER SERVICE STORED PROCEDURES
-- ============================================

USE task_manager_db;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GetManagerDepartment$$
CREATE PROCEDURE sp_GetManagerDepartment(
    IN p_manager_id INT
)
BEGIN
    SELECT department_id FROM users WHERE id = p_manager_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetDepartmentName$$
CREATE PROCEDURE sp_GetDepartmentName(
    IN p_dept_id INT
)
BEGIN
    SELECT name FROM departments WHERE id = p_dept_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTeamMemberCount$$
CREATE PROCEDURE sp_GetTeamMemberCount(
    IN p_dept_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM users 
    WHERE department_id = p_dept_id AND is_active = 1;
END$$

DROP PROCEDURE IF EXISTS sp_GetManagerActiveProjectsCount$$
CREATE PROCEDURE sp_GetManagerActiveProjectsCount(
    IN p_manager_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM projects 
    WHERE manager_id = p_manager_id AND is_active = 1 AND status IN ('planning', 'active');
END$$

DROP PROCEDURE IF EXISTS sp_GetProjectsOnTrackCount$$
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

DROP PROCEDURE IF EXISTS sp_GetProjectsAtRiskCount$$
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

DROP PROCEDURE IF EXISTS sp_GetProjectsDelayedCount$$
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

DROP PROCEDURE IF EXISTS sp_GetTotalTasksCount$$
CREATE PROCEDURE sp_GetTotalTasksCount(
    IN p_manager_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM tasks t
    INNER JOIN projects p ON t.project_id = p.id
    WHERE p.manager_id = p_manager_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetCompletedThisWeekCount$$
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

DROP PROCEDURE IF EXISTS sp_GetOverdueTasksCount$$
CREATE PROCEDURE sp_GetOverdueTasksCount(
    IN p_manager_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM tasks t
    INNER JOIN projects p ON t.project_id = p.id
    WHERE p.manager_id = p_manager_id
      AND t.status_id != 4
      AND t.due_date < CURDATE();
END$$

DROP PROCEDURE IF EXISTS sp_GetInProgressTasksCount$$
CREATE PROCEDURE sp_GetInProgressTasksCount(
    IN p_manager_id INT
)
BEGIN
    SELECT COUNT(*) as count FROM tasks t
    INNER JOIN projects p ON t.project_id = p.id
    WHERE p.manager_id = p_manager_id
      AND t.status_id IN (2, 3);
END$$

DROP PROCEDURE IF EXISTS sp_GetTeamCompletionRate$$
CREATE PROCEDURE sp_GetTeamCompletionRate(
    IN p_dept_id INT
)
BEGIN
    SELECT 
        COALESCE(
            (SUM(CASE WHEN t.status_id = 4 THEN 1 ELSE 0 END) * 100.0 / COUNT(*)), 
            0
        ) as completion_rate
    FROM tasks t
    INNER JOIN users u ON t.assigned_to = u.id
    WHERE u.department_id = p_dept_id
      AND t.created_at >= DATE_SUB(CURDATE(), INTERVAL 30 DAY);
END$$

DROP PROCEDURE IF EXISTS sp_GetAverageTaskTime$$
CREATE PROCEDURE sp_GetAverageTaskTime(
    IN p_dept_id INT
)
BEGIN
    SELECT COALESCE(AVG(DATEDIFF(t.updated_at, t.created_at)), 0) as avg_days
    FROM tasks t
    INNER JOIN users u ON t.assigned_to = u.id
    WHERE u.department_id = p_dept_id
      AND t.status_id = 4
      AND t.updated_at >= DATE_SUB(CURDATE(), INTERVAL 30 DAY);
END$$

DROP PROCEDURE IF EXISTS sp_GetRecentActivitiesForManager$$
CREATE PROCEDURE sp_GetRecentActivitiesForManager(
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

DROP PROCEDURE IF EXISTS sp_GetManagedProjects$$
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

DELIMITER ;

SELECT 'Manager service stored procedures created successfully!' as Status;

-- Fix All Stored Procedure Name Conflicts
-- This ensures admin dashboard procedures don't have parameters
-- And manager dashboard procedures have different names

DELIMITER $$

-- ============================================
-- ADMIN DASHBOARD PROCEDURES (No Parameters)
-- ============================================

-- Get active projects count (admin version)
DROP PROCEDURE IF EXISTS sp_GetActiveProjectsCount$$
CREATE PROCEDURE sp_GetActiveProjectsCount()
BEGIN
    SELECT COUNT(*) as count FROM projects 
    WHERE is_active = 1 AND status IN ('planning', 'active');
END$$

-- Get top performers (admin version - all departments)
DROP PROCEDURE IF EXISTS sp_GetTopPerformers$$
CREATE PROCEDURE sp_GetTopPerformers()
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.profile_image, u.job_title, d.name as department_name,
           COUNT(DISTINCT t.id) as total,
           SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) as completed
    FROM users u
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN tasks t ON u.id = t.assigned_to
    LEFT JOIN statuses s ON t.status_id = s.id
    WHERE u.is_active = 1 AND u.role_id != 1
    GROUP BY u.id
    HAVING total > 0
    ORDER BY completed DESC, total DESC
    LIMIT 5;
END$$

-- ============================================
-- MANAGER DASHBOARD PROCEDURES (With Parameters)
-- ============================================

-- Get active projects count by manager
DROP PROCEDURE IF EXISTS sp_GetActiveProjectsCountByManager$$
CREATE PROCEDURE sp_GetActiveProjectsCountByManager(IN p_manager_id INT)
BEGIN
    SELECT COUNT(*) as count FROM projects 
    WHERE manager_id = p_manager_id AND is_active = 1 AND status IN ('planning', 'active');
END$$

-- Get top performers by department
DROP PROCEDURE IF EXISTS sp_GetTopPerformersByDepartment$$
CREATE PROCEDURE sp_GetTopPerformersByDepartment(
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

DELIMITER ;

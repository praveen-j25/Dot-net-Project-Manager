-- =====================================================
-- FIX: sp_GetTopPerformers Procedure
-- =====================================================
-- Issue: The procedure signature doesn't match what the
-- ManagerService expects. The C# code calls it with
-- parameters (p_dept_id, p_limit) and expects columns:
-- user_id, user_name, employee_id, total_tasks, 
-- completed_tasks, on_time_tasks
-- =====================================================

USE task_manager_db;

DELIMITER $$

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

DELIMITER ;

-- Add missing Manager Service stored procedures
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

DELIMITER ;

SELECT 'Manager procedures added successfully!' as Status;

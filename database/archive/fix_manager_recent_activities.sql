-- Fix manager recent activities to show who actually created/updated the task
USE task_manager_db;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GetManagerRecentActivities$$

CREATE PROCEDURE sp_GetManagerRecentActivities(
    IN p_manager_id INT,
    IN p_limit INT
)
BEGIN
    -- Show recent task updates from manager's projects
    -- Show the person who created/assigned the task, not the assignee
    SELECT 
        CONCAT(creator.first_name, ' ', creator.last_name) as user_name,
        CASE 
            WHEN t.status_id = 4 THEN 'completed'
            WHEN t.created_at = t.updated_at THEN 'assigned'
            ELSE 'updated'
        END as action,
        'Task' as entity_type,
        t.title as entity_name,
        t.updated_at as created_at
    FROM tasks t
    INNER JOIN projects p ON t.project_id = p.id
    LEFT JOIN users creator ON COALESCE(t.assigned_by, t.created_by) = creator.id
    WHERE p.manager_id = p_manager_id
      AND t.updated_at IS NOT NULL
    ORDER BY t.updated_at DESC
    LIMIT p_limit;
END$$

DELIMITER ;

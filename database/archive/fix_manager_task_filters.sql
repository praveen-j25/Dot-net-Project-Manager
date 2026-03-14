-- Fix sp_GetTasksForManager to support filters
USE task_manager_db;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GetTasksForManager$$
CREATE PROCEDURE sp_GetTasksForManager(
    IN p_manager_id INT,
    IN p_status_id INT,
    IN p_priority_id INT,
    IN p_project_id INT
)
BEGIN
    SELECT t.id, t.title, t.description, t.due_date, t.task_type, t.progress,
           c.name as category_name, p.name as priority_name, s.name as status_name,
           CONCAT(u.first_name, ' ', u.last_name) as assignee_name,
           proj.name as project_name, proj.code as project_code
    FROM tasks t
    LEFT JOIN categories c ON t.category_id = c.id
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN users u ON t.assigned_to = u.id
    LEFT JOIN projects proj ON t.project_id = proj.id
    WHERE proj.manager_id = p_manager_id
      AND (p_status_id IS NULL OR t.status_id = p_status_id)
      AND (p_priority_id IS NULL OR t.priority_id = p_priority_id)
      AND (p_project_id IS NULL OR t.project_id = p_project_id)
    ORDER BY t.due_date ASC;
END$$

DELIMITER ;

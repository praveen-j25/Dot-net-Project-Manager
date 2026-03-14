-- Fix sp_GetTaskById to return creator_first and creator_last columns
-- The C# code expects these separate columns, not a concatenated creator_name

USE task_manager_db;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GetTaskById$$

CREATE PROCEDURE sp_GetTaskById(
    IN p_task_id INT
)
BEGIN
    SELECT t.id, t.title, t.description, t.due_date, t.created_at, t.updated_at,
           t.created_by, t.assigned_to, t.hourly_rate,
           c.name as category_name, 
           p.name as priority_name, 
           s.name as status_name,
           CONCAT(u.first_name, ' ', u.last_name) as assignee_name,
           c_user.first_name as creator_first,
           c_user.last_name as creator_last
    FROM tasks t
    LEFT JOIN categories c ON t.category_id = c.id
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN users u ON t.assigned_to = u.id
    LEFT JOIN users c_user ON t.created_by = c_user.id
    WHERE t.id = p_task_id;
END$$

DELIMITER ;

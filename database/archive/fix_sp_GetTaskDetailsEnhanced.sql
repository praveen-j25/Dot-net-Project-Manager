-- Fix sp_GetTaskDetailsEnhanced to include assigned_by_name column
-- This adds the missing assigned_by_name that the C# code expects

USE task_manager_db;

DELIMITER $

DROP PROCEDURE IF EXISTS sp_GetTaskDetailsEnhanced$

CREATE PROCEDURE sp_GetTaskDetailsEnhanced(
    IN p_task_id INT
)
BEGIN
    SELECT t.id, t.title, t.description, t.due_date, t.start_date, t.completed_date, 
           t.task_type, t.progress, t.estimated_hours, t.actual_hours, t.is_billable, t.tags,
           t.created_at, t.updated_at, t.project_id, t.assigned_to, t.assigned_at, t.created_by,
           c.name as category_name, 
           p.name as priority_name, 
           s.name as status_name,
           proj.name as project_name, 
           proj.code as project_code,
           CONCAT(u.first_name, ' ', u.last_name) as assignee_name, 
           u.profile_image as assignee_image,
           CONCAT(ab.first_name, ' ', ab.last_name) as assigned_by_name,
           CONCAT(cr.first_name, ' ', cr.last_name) as creator_name
    FROM tasks t
    LEFT JOIN categories c ON t.category_id = c.id
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN projects proj ON t.project_id = proj.id
    LEFT JOIN users u ON t.assigned_to = u.id
    LEFT JOIN users ab ON t.assigned_by = ab.id
    LEFT JOIN users cr ON t.created_by = cr.id
    WHERE t.id = p_task_id;
END$

DELIMITER ;

SELECT 'sp_GetTaskDetailsEnhanced fixed - assigned_by_name column added' as Status;

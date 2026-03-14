DELIMITER //

DROP PROCEDURE IF EXISTS sp_GetTaskById //

CREATE PROCEDURE sp_GetTaskById(IN p_task_id INT)
BEGIN
    SELECT 
        t.id, 
        t.title, 
        t.description, 
        t.category_id, 
        c.name as category_name, 
        t.priority_id, 
        pr.name as priority_name, 
        t.status_id, 
        s.name as status_name, 
        t.due_date, 
        t.created_at, 
        t.updated_at, 
        t.created_by, 
        u.first_name as creator_first, 
        u.last_name as creator_last, 
        t.hourly_rate,
        t.assigned_to,
        t.project_id,
        p.manager_id as project_manager_id
    FROM Tasks t
    LEFT JOIN Categories c ON t.category_id = c.id
    JOIN Priorities pr ON t.priority_id = pr.id
    JOIN Statuses s ON t.status_id = s.id
    JOIN Users u ON t.created_by = u.id
    LEFT JOIN Projects p ON t.project_id = p.id
    WHERE t.id = p_task_id;
END //

DELIMITER ;

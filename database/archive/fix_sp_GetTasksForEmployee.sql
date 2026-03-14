-- Fix sp_GetTasksForEmployee to accept all required parameters
-- This procedure is called by TaskServiceExtended.GetEmployeeTasksAsync

USE task_manager_db;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GetTasksForEmployee$$

CREATE PROCEDURE sp_GetTasksForEmployee(
    IN p_user_id INT,
    IN p_status_id INT,
    IN p_priority_id INT,
    IN p_sort_by VARCHAR(50)
)
BEGIN
    -- Build the base query
    SET @sql = '
        SELECT t.id, t.title, t.description, t.due_date, t.task_type, t.progress, t.status_id,
               c.name as category_name, p.name as priority_name, s.name as status_name,
               proj.name as project_name
        FROM tasks t
        LEFT JOIN categories c ON t.category_id = c.id
        LEFT JOIN priorities p ON t.priority_id = p.id
        LEFT JOIN statuses s ON t.status_id = s.id
        LEFT JOIN projects proj ON t.project_id = proj.id
        WHERE t.assigned_to = ?';
    
    -- Add status filter if provided
    IF p_status_id IS NOT NULL THEN
        SET @sql = CONCAT(@sql, ' AND t.status_id = ', p_status_id);
    END IF;
    
    -- Add priority filter if provided
    IF p_priority_id IS NOT NULL THEN
        SET @sql = CONCAT(@sql, ' AND t.priority_id = ', p_priority_id);
    END IF;
    
    -- Add sorting
    IF p_sort_by = 'due_date' THEN
        SET @sql = CONCAT(@sql, ' ORDER BY t.due_date ASC');
    ELSEIF p_sort_by = 'priority' THEN
        SET @sql = CONCAT(@sql, ' ORDER BY p.level DESC');
    ELSEIF p_sort_by = 'status' THEN
        SET @sql = CONCAT(@sql, ' ORDER BY s.sort_order');
    ELSE
        SET @sql = CONCAT(@sql, ' ORDER BY t.created_at DESC');
    END IF;
    
    -- Prepare and execute
    SET @user_id = p_user_id;
    PREPARE stmt FROM @sql;
    EXECUTE stmt USING @user_id;
    DEALLOCATE PREPARE stmt;
END$$

DELIMITER ;

-- Fix missing task-related stored procedures
USE task_manager_db;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GetTasksForAdmin$$
CREATE PROCEDURE sp_GetTasksForAdmin(
    IN p_status_id INT,
    IN p_priority_id INT,
    IN p_project_id INT,
    IN p_assigned_to INT
)
BEGIN
    SELECT t.id, t.title, t.description, t.due_date, t.task_type, t.progress,
           c.name as category_name, p.name as priority_name, s.name as status_name,
           CONCAT(u.first_name, ' ', u.last_name) as assignee_name,
           proj.name as project_name
    FROM tasks t
    LEFT JOIN categories c ON t.category_id = c.id
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN users u ON t.assigned_to = u.id
    LEFT JOIN projects proj ON t.project_id = proj.id
    WHERE (p_status_id IS NULL OR t.status_id = p_status_id)
      AND (p_priority_id IS NULL OR t.priority_id = p_priority_id)
      AND (p_project_id IS NULL OR t.project_id = p_project_id)
      AND (p_assigned_to IS NULL OR t.assigned_to = p_assigned_to)
    ORDER BY t.created_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskAssignmentForm$$
CREATE PROCEDURE sp_GetTaskAssignmentForm(
    IN p_task_id INT
)
BEGIN
    SELECT id, title, description, project_id, category_id, priority_id, status_id, 
           task_type, assigned_to, start_date, due_date, estimated_hours, is_billable, hourly_rate, tags
    FROM tasks WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_AssignTask$$
CREATE PROCEDURE sp_AssignTask(
    IN p_title VARCHAR(255),
    IN p_description TEXT,
    IN p_project_id INT,
    IN p_category_id INT,
    IN p_priority_id INT,
    IN p_status_id INT,
    IN p_task_type VARCHAR(50),
    IN p_assigned_to INT,
    IN p_assigned_by INT,
    IN p_assigned_at DATETIME,
    IN p_start_date DATE,
    IN p_due_date DATE,
    IN p_estimated_hours DECIMAL(5,2),
    IN p_is_billable BOOLEAN,
    IN p_hourly_rate DECIMAL(10,2),
    IN p_tags TEXT,
    IN p_created_by INT,
    IN p_created_at DATETIME,
    IN p_updated_at DATETIME
)
BEGIN
    INSERT INTO tasks (title, description, project_id, category_id, priority_id, status_id, 
                      task_type, assigned_to, assigned_by, assigned_at, start_date, due_date, 
                      estimated_hours, is_billable, hourly_rate, tags, created_by, created_at, updated_at)
    VALUES (p_title, p_description, p_project_id, p_category_id, p_priority_id, p_status_id,
            p_task_type, p_assigned_to, p_assigned_by, p_assigned_at, p_start_date, p_due_date,
            p_estimated_hours, p_is_billable, p_hourly_rate, p_tags, p_created_by, p_created_at, p_updated_at);
    
    SELECT LAST_INSERT_ID() as id;
END$$

DROP PROCEDURE IF EXISTS sp_GetOldAssignee$$
CREATE PROCEDURE sp_GetOldAssignee(
    IN p_task_id INT
)
BEGIN
    SELECT assigned_to FROM tasks WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateTaskAssignment$$
CREATE PROCEDURE sp_UpdateTaskAssignment(
    IN p_id INT,
    IN p_title VARCHAR(255),
    IN p_description TEXT,
    IN p_project_id INT,
    IN p_category_id INT,
    IN p_priority_id INT,
    IN p_status_id INT,
    IN p_task_type VARCHAR(50),
    IN p_assigned_to INT,
    IN p_assigned_by INT,
    IN p_assigned_at DATETIME,
    IN p_start_date DATE,
    IN p_due_date DATE,
    IN p_estimated_hours DECIMAL(5,2),
    IN p_is_billable BOOLEAN,
    IN p_hourly_rate DECIMAL(10,2),
    IN p_tags TEXT,
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE tasks 
    SET title = p_title, 
        description = p_description, 
        project_id = p_project_id, 
        category_id = p_category_id, 
        priority_id = p_priority_id, 
        status_id = p_status_id,
        task_type = p_task_type, 
        assigned_to = p_assigned_to, 
        assigned_by = p_assigned_by, 
        assigned_at = p_assigned_at,
        start_date = p_start_date, 
        due_date = p_due_date, 
        estimated_hours = p_estimated_hours, 
        is_billable = p_is_billable, 
        hourly_rate = p_hourly_rate, 
        tags = p_tags, 
        updated_at = p_updated_at
    WHERE id = p_id;
END$$

DELIMITER ;

SELECT 'Missing task procedures created successfully!' as Status;

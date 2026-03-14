-- =====================================================
-- Task Manager - Stored Procedures
-- MySQL Stored Procedures for Business Logic
-- =====================================================

USE task_manager_db;

DELIMITER $$

-- =====================================================
-- SP: User Authentication
-- =====================================================
DROP PROCEDURE IF EXISTS sp_authenticate_user$$
CREATE PROCEDURE sp_authenticate_user(
    IN p_email VARCHAR(100)
)
BEGIN
    SELECT 
        u.id, u.first_name, u.last_name, u.email, u.password, 
        u.phone, u.is_active, u.is_verified, u.created_at, u.last_login,
        u.role_id, u.job_title, u.employee_id, u.department_id, u.team_id,
        r.name as role_name,
        d.name as department_name,
        t.name as team_name
    FROM users u
    LEFT JOIN roles r ON u.role_id = r.id
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    WHERE u.email = p_email AND u.is_active = 1;
END$$

-- =====================================================
-- SP: Update Last Login
-- =====================================================
DROP PROCEDURE IF EXISTS sp_update_last_login$$
CREATE PROCEDURE sp_update_last_login(
    IN p_user_id INT
)
BEGIN
    UPDATE users 
    SET last_login = NOW() 
    WHERE id = p_user_id;
END$$

-- =====================================================
-- SP: Create User
-- =====================================================
DROP PROCEDURE IF EXISTS sp_create_user$$
CREATE PROCEDURE sp_create_user(
    IN p_first_name VARCHAR(50),
    IN p_last_name VARCHAR(50),
    IN p_email VARCHAR(100),
    IN p_password VARCHAR(255),
    IN p_phone VARCHAR(20),
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_job_title VARCHAR(100),
    OUT p_user_id INT,
    OUT p_success BOOLEAN,
    OUT p_message VARCHAR(255)
)
BEGIN
    DECLARE email_exists INT DEFAULT 0;
    
    -- Check if email exists
    SELECT COUNT(*) INTO email_exists FROM users WHERE email = p_email;
    
    IF email_exists > 0 THEN
        SET p_success = FALSE;
        SET p_message = 'Email already exists';
        SET p_user_id = 0;
    ELSE
        INSERT INTO users (
            first_name, last_name, email, password, phone, 
            role_id, department_id, job_title, is_active, is_verified, created_at
        ) VALUES (
            p_first_name, p_last_name, p_email, p_password, p_phone,
            p_role_id, p_department_id, p_job_title, 1, 1, NOW()
        );
        
        SET p_user_id = LAST_INSERT_ID();
        SET p_success = TRUE;
        SET p_message = 'User created successfully';
    END IF;
END$$

-- =====================================================
-- SP: Get User Dashboard Stats
-- =====================================================
DROP PROCEDURE IF EXISTS sp_get_user_dashboard$$
CREATE PROCEDURE sp_get_user_dashboard(
    IN p_user_id INT
)
BEGIN
    SELECT 
        COUNT(*) as total_tasks,
        SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) as completed_tasks,
        SUM(CASE WHEN s.name = 'in_progress' THEN 1 ELSE 0 END) as in_progress_tasks,
        SUM(CASE WHEN s.name = 'todo' THEN 1 ELSE 0 END) as pending_tasks,
        SUM(CASE WHEN t.due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled') THEN 1 ELSE 0 END) as overdue_tasks,
        SUM(CASE WHEN pr.name = 'high' OR pr.name = 'critical' THEN 1 ELSE 0 END) as high_priority_tasks
    FROM tasks t
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN priorities pr ON t.priority_id = pr.id
    WHERE t.assigned_to = p_user_id;
END$$

-- =====================================================
-- SP: Get Tasks with Filters
-- =====================================================
DROP PROCEDURE IF EXISTS sp_get_tasks$$
CREATE PROCEDURE sp_get_tasks(
    IN p_user_id INT,
    IN p_role_id INT,
    IN p_status_id INT,
    IN p_priority_id INT,
    IN p_category_id INT,
    IN p_project_id INT
)
BEGIN
    SELECT 
        t.id, t.title, t.description, t.due_date, t.start_date, t.completed_date,
        t.estimated_hours, t.actual_hours, t.progress, t.created_at, t.updated_at,
        t.task_type, t.is_billable, t.tags,
        s.id as status_id, s.name as status_name, s.display_name as status_display, s.color as status_color,
        pr.id as priority_id, pr.name as priority_name, pr.display_name as priority_display, pr.color as priority_color,
        c.id as category_id, c.name as category_name,
        p.id as project_id, p.name as project_name, p.code as project_code,
        CONCAT(assignee.first_name, ' ', assignee.last_name) as assignee_name,
        CONCAT(creator.first_name, ' ', creator.last_name) as creator_name,
        DATEDIFF(t.due_date, CURDATE()) as days_remaining
    FROM tasks t
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN priorities pr ON t.priority_id = pr.id
    LEFT JOIN categories c ON t.category_id = c.id
    LEFT JOIN projects p ON t.project_id = p.id
    LEFT JOIN users assignee ON t.assigned_to = assignee.id
    LEFT JOIN users creator ON t.created_by = creator.id
    WHERE 
        (p_role_id = 1 OR t.assigned_to = p_user_id OR t.created_by = p_user_id)
        AND (p_status_id IS NULL OR t.status_id = p_status_id)
        AND (p_priority_id IS NULL OR t.priority_id = p_priority_id)
        AND (p_category_id IS NULL OR t.category_id = p_category_id)
        AND (p_project_id IS NULL OR t.project_id = p_project_id)
    ORDER BY 
        CASE WHEN pr.name = 'critical' THEN 1 
             WHEN pr.name = 'high' THEN 2 
             WHEN pr.name = 'medium' THEN 3 
             ELSE 4 END,
        t.due_date ASC;
END$$

-- =====================================================
-- SP: Create Task
-- =====================================================
DROP PROCEDURE IF EXISTS sp_create_task$$
CREATE PROCEDURE sp_create_task(
    IN p_title VARCHAR(200),
    IN p_description TEXT,
    IN p_category_id INT,
    IN p_priority_id INT,
    IN p_status_id INT,
    IN p_due_date DATE,
    IN p_start_date DATE,
    IN p_estimated_hours DECIMAL(10,2),
    IN p_assigned_to INT,
    IN p_created_by INT,
    IN p_project_id INT,
    IN p_task_type VARCHAR(50),
    OUT p_task_id INT
)
BEGIN
    INSERT INTO tasks (
        title, description, category_id, priority_id, status_id,
        due_date, start_date, estimated_hours, assigned_to, created_by,
        project_id, task_type, progress, created_at, updated_at
    ) VALUES (
        p_title, p_description, p_category_id, p_priority_id, p_status_id,
        p_due_date, p_start_date, p_estimated_hours, p_assigned_to, p_created_by,
        p_project_id, p_task_type, 0, NOW(), NOW()
    );
    
    SET p_task_id = LAST_INSERT_ID();
    
    -- Create notification for assignee
    IF p_assigned_to IS NOT NULL AND p_assigned_to != p_created_by THEN
        INSERT INTO notifications (user_id, title, message, type, reference_type, reference_id)
        VALUES (
            p_assigned_to,
            'New Task Assigned',
            CONCAT('You have been assigned a new task: ', p_title),
            'task_assigned',
            'task',
            p_task_id
        );
    END IF;
END$$

-- =====================================================
-- SP: Update Task
-- =====================================================
DROP PROCEDURE IF EXISTS sp_update_task$$
CREATE PROCEDURE sp_update_task(
    IN p_task_id INT,
    IN p_title VARCHAR(200),
    IN p_description TEXT,
    IN p_category_id INT,
    IN p_priority_id INT,
    IN p_status_id INT,
    IN p_due_date DATE,
    IN p_start_date DATE,
    IN p_estimated_hours DECIMAL(10,2),
    IN p_actual_hours DECIMAL(10,2),
    IN p_progress INT,
    IN p_assigned_to INT
)
BEGIN
    DECLARE old_status_id INT;
    DECLARE old_assigned_to INT;
    
    -- Get old values
    SELECT status_id, assigned_to INTO old_status_id, old_assigned_to
    FROM tasks WHERE id = p_task_id;
    
    -- Update task
    UPDATE tasks SET
        title = p_title,
        description = p_description,
        category_id = p_category_id,
        priority_id = p_priority_id,
        status_id = p_status_id,
        due_date = p_due_date,
        start_date = p_start_date,
        estimated_hours = p_estimated_hours,
        actual_hours = p_actual_hours,
        progress = p_progress,
        assigned_to = p_assigned_to,
        updated_at = NOW(),
        completed_date = CASE WHEN p_status_id = 7 THEN NOW() ELSE completed_date END
    WHERE id = p_task_id;
    
    -- Notify if assignee changed
    IF p_assigned_to != old_assigned_to AND p_assigned_to IS NOT NULL THEN
        INSERT INTO notifications (user_id, title, message, type, reference_type, reference_id)
        VALUES (
            p_assigned_to,
            'Task Reassigned',
            CONCAT('Task "', p_title, '" has been assigned to you'),
            'task_assigned',
            'task',
            p_task_id
        );
    END IF;
    
    -- Notify if status changed to completed
    IF p_status_id = 7 AND old_status_id != 7 THEN
        INSERT INTO notifications (user_id, title, message, type, reference_type, reference_id)
        SELECT 
            created_by,
            'Task Completed',
            CONCAT('Task "', p_title, '" has been completed'),
            'task_updated',
            'task',
            p_task_id
        FROM tasks WHERE id = p_task_id AND created_by != p_assigned_to;
    END IF;
END$$

-- =====================================================
-- SP: Delete Task
-- =====================================================
DROP PROCEDURE IF EXISTS sp_delete_task$$
CREATE PROCEDURE sp_delete_task(
    IN p_task_id INT,
    IN p_user_id INT,
    OUT p_success BOOLEAN,
    OUT p_message VARCHAR(255)
)
BEGIN
    DECLARE task_exists INT DEFAULT 0;
    DECLARE user_role_id INT;
    
    SELECT role_id INTO user_role_id FROM users WHERE id = p_user_id;
    SELECT COUNT(*) INTO task_exists FROM tasks WHERE id = p_task_id;
    
    IF task_exists = 0 THEN
        SET p_success = FALSE;
        SET p_message = 'Task not found';
    ELSEIF user_role_id NOT IN (1, 2) THEN
        SET p_success = FALSE;
        SET p_message = 'Insufficient permissions';
    ELSE
        DELETE FROM tasks WHERE id = p_task_id;
        SET p_success = TRUE;
        SET p_message = 'Task deleted successfully';
    END IF;
END$$

-- =====================================================
-- SP: Get Admin Dashboard Stats
-- =====================================================
DROP PROCEDURE IF EXISTS sp_get_admin_dashboard$$
CREATE PROCEDURE sp_get_admin_dashboard()
BEGIN
    -- Overall statistics
    SELECT 
        (SELECT COUNT(*) FROM users WHERE is_active = 1) as total_users,
        (SELECT COUNT(*) FROM projects WHERE is_active = 1) as total_projects,
        (SELECT COUNT(*) FROM tasks) as total_tasks,
        (SELECT COUNT(*) FROM tasks WHERE status_id = 7) as completed_tasks,
        (SELECT COUNT(*) FROM tasks WHERE due_date < CURDATE() AND status_id NOT IN (7, 8)) as overdue_tasks,
        (SELECT COUNT(*) FROM tasks WHERE status_id = 3) as in_progress_tasks;
    
    -- Tasks by status
    SELECT 
        s.display_name as status,
        s.color,
        COUNT(t.id) as count
    FROM statuses s
    LEFT JOIN tasks t ON s.id = t.status_id
    GROUP BY s.id, s.display_name, s.color, s.sort_order
    ORDER BY s.sort_order;
    
    -- Tasks by priority
    SELECT 
        p.display_name as priority,
        p.color,
        COUNT(t.id) as count
    FROM priorities p
    LEFT JOIN tasks t ON p.id = t.priority_id
    GROUP BY p.id, p.display_name, p.color
    ORDER BY p.id;
    
    -- Recent tasks
    SELECT 
        t.id, t.title, t.due_date,
        s.display_name as status,
        pr.display_name as priority,
        CONCAT(u.first_name, ' ', u.last_name) as assignee
    FROM tasks t
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN priorities pr ON t.priority_id = pr.id
    LEFT JOIN users u ON t.assigned_to = u.id
    ORDER BY t.created_at DESC
    LIMIT 10;
END$$

-- =====================================================
-- SP: Get Project Statistics
-- =====================================================
DROP PROCEDURE IF EXISTS sp_get_project_stats$$
CREATE PROCEDURE sp_get_project_stats(
    IN p_project_id INT
)
BEGIN
    SELECT 
        p.id, p.name, p.code, p.status, p.priority,
        p.start_date, p.end_date, p.budget,
        COUNT(t.id) as total_tasks,
        SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) as completed_tasks,
        SUM(CASE WHEN s.name = 'in_progress' THEN 1 ELSE 0 END) as in_progress_tasks,
        SUM(CASE WHEN t.due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled') THEN 1 ELSE 0 END) as overdue_tasks,
        SUM(t.estimated_hours) as total_estimated_hours,
        SUM(t.actual_hours) as total_actual_hours,
        ROUND(SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) * 100.0 / NULLIF(COUNT(t.id), 0), 1) as progress_percent
    FROM projects p
    LEFT JOIN tasks t ON t.project_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    WHERE p.id = p_project_id
    GROUP BY p.id, p.name, p.code, p.status, p.priority, p.start_date, p.end_date, p.budget;
END$$

-- =====================================================
-- SP: Get User Performance Report
-- =====================================================
DROP PROCEDURE IF EXISTS sp_get_user_performance$$
CREATE PROCEDURE sp_get_user_performance(
    IN p_user_id INT,
    IN p_start_date DATE,
    IN p_end_date DATE
)
BEGIN
    SELECT 
        u.id, u.first_name, u.last_name, u.email, u.job_title,
        r.name as role_name,
        d.name as department_name,
        COUNT(t.id) as total_tasks,
        SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) as completed_tasks,
        SUM(CASE WHEN t.completed_date <= t.due_date THEN 1 ELSE 0 END) as on_time_completions,
        SUM(t.estimated_hours) as total_estimated_hours,
        SUM(t.actual_hours) as total_actual_hours,
        ROUND(AVG(t.progress), 1) as avg_progress,
        ROUND(SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) * 100.0 / NULLIF(COUNT(t.id), 0), 1) as completion_rate
    FROM users u
    LEFT JOIN roles r ON u.role_id = r.id
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN tasks t ON t.assigned_to = u.id 
        AND t.created_at BETWEEN p_start_date AND p_end_date
    LEFT JOIN statuses s ON t.status_id = s.id
    WHERE u.id = p_user_id
    GROUP BY u.id, u.first_name, u.last_name, u.email, u.job_title, r.name, d.name;
END$$

DELIMITER ;

-- =====================================================
-- Grant Execute Permissions
-- =====================================================
-- GRANT EXECUTE ON PROCEDURE task_manager_db.* TO 'your_app_user'@'localhost';


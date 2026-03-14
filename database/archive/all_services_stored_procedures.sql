-- ============================================
-- COMPLETE STORED PROCEDURES FOR ALL SERVICES
-- Professional-grade stored procedures replacing ALL inline SQL
-- Task Manager MVC Application
-- ============================================

USE task_manager_db;

DELIMITER $$

-- ============================================
-- USER SERVICE PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_GetUsersWithFilters$$
CREATE PROCEDURE sp_GetUsersWithFilters(
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_team_id INT,
    IN p_is_active BOOLEAN
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.email, u.employee_id, u.job_title, u.profile_image, u.is_active, u.last_login,
           r.name as role_name, d.name as department_name, t.name as team_name,
           (SELECT COUNT(*) FROM tasks WHERE assigned_to = u.id) as task_count
    FROM users u
    LEFT JOIN roles r ON u.role_id = r.id
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    WHERE (p_role_id IS NULL OR u.role_id = p_role_id)
      AND (p_department_id IS NULL OR u.department_id = p_department_id)
      AND (p_team_id IS NULL OR u.team_id = p_team_id)
      AND (p_is_active IS NULL OR u.is_active = p_is_active)
    ORDER BY u.first_name, u.last_name;
END$$

DROP PROCEDURE IF EXISTS sp_GetUsersGroupedByTeam$$
CREATE PROCEDURE sp_GetUsersGroupedByTeam(
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_is_active BOOLEAN
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.email, u.employee_id, u.job_title, u.profile_image, 
           u.is_active, u.last_login, u.team_id,
           r.name as role_name, d.name as department_name, t.name as team_name,
           CONCAT(m.first_name, ' ', m.last_name) as manager_name,
           (SELECT COUNT(*) FROM tasks WHERE assigned_to = u.id) as task_count
    FROM users u
    LEFT JOIN roles r ON u.role_id = r.id
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    LEFT JOIN users m ON t.manager_id = m.id
    WHERE (p_role_id IS NULL OR u.role_id = p_role_id)
      AND (p_department_id IS NULL OR u.department_id = p_department_id)
      AND (p_is_active IS NULL OR u.is_active = p_is_active)
    ORDER BY t.name, u.first_name, u.last_name;
END$$

DROP PROCEDURE IF EXISTS sp_GetUserForEdit$$
CREATE PROCEDURE sp_GetUserForEdit(
    IN p_user_id INT
)
BEGIN
    SELECT id, first_name, last_name, email, phone, employee_id, job_title, 
           role_id, department_id, team_id, reports_to, hire_date, is_active
    FROM users WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_CreateUser$$
CREATE PROCEDURE sp_CreateUser(
    IN p_first_name VARCHAR(100),
    IN p_last_name VARCHAR(100),
    IN p_email VARCHAR(255),
    IN p_password VARCHAR(255),
    IN p_phone VARCHAR(20),
    IN p_employee_id VARCHAR(50),
    IN p_job_title VARCHAR(100),
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_team_id INT,
    IN p_reports_to INT,
    IN p_hire_date DATE,
    IN p_is_active BOOLEAN,
    IN p_created_at DATETIME
)
BEGIN
    INSERT INTO users (first_name, last_name, email, password, phone, employee_id, job_title, 
                       role_id, department_id, team_id, reports_to, hire_date, is_active, is_verified, created_at)
    VALUES (p_first_name, p_last_name, p_email, p_password, p_phone, p_employee_id, p_job_title, 
            p_role_id, p_department_id, p_team_id, p_reports_to, p_hire_date, p_is_active, 1, p_created_at);
    
    SELECT LAST_INSERT_ID() as id;
END$$

DROP PROCEDURE IF EXISTS sp_GetNextEmployeeId$$
CREATE PROCEDURE sp_GetNextEmployeeId()
BEGIN
    SELECT employee_id FROM users 
    WHERE employee_id LIKE 'EMP%' 
    ORDER BY CAST(SUBSTRING(employee_id, 4) AS UNSIGNED) DESC 
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateUser$$
CREATE PROCEDURE sp_UpdateUser(
    IN p_id INT,
    IN p_first_name VARCHAR(100),
    IN p_last_name VARCHAR(100),
    IN p_email VARCHAR(255),
    IN p_phone VARCHAR(20),
    IN p_employee_id VARCHAR(50),
    IN p_job_title VARCHAR(100),
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_team_id INT,
    IN p_reports_to INT,
    IN p_hire_date DATE,
    IN p_is_active BOOLEAN,
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE users SET 
        first_name = p_first_name, last_name = p_last_name, email = p_email, phone = p_phone,
        employee_id = p_employee_id, job_title = p_job_title, role_id = p_role_id,
        department_id = p_department_id, team_id = p_team_id, reports_to = p_reports_to,
        hire_date = p_hire_date, is_active = p_is_active, updated_at = p_updated_at
    WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS sp_DeleteUser$$
CREATE PROCEDURE sp_DeleteUser(
    IN p_user_id INT
)
BEGIN
    -- Soft delete by setting is_active = false
    UPDATE users SET is_active = 0 WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetEmployeesForAssignment$$
CREATE PROCEDURE sp_GetEmployeesForAssignment()
BEGIN
    SELECT id, CONCAT(first_name, ' ', last_name) as full_name, job_title
    FROM users 
    WHERE is_active = 1 AND role_id != 1 
    ORDER BY first_name, last_name;
END$$

DROP PROCEDURE IF EXISTS sp_GetEmployeeSummaries$$
CREATE PROCEDURE sp_GetEmployeeSummaries(
    IN p_department_id INT,
    IN p_team_id INT
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.profile_image, u.job_title,
           d.name as department_name, t.name as team_name,
           COUNT(DISTINCT tk.id) as total_tasks,
           SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) as completed_tasks,
           SUM(CASE WHEN tk.due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled') THEN 1 ELSE 0 END) as overdue_tasks
    FROM users u
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    LEFT JOIN tasks tk ON tk.assigned_to = u.id
    LEFT JOIN statuses s ON tk.status_id = s.id
    WHERE u.is_active = 1 AND u.role_id = 3
      AND (p_department_id IS NULL OR u.department_id = p_department_id)
      AND (p_team_id IS NULL OR u.team_id = p_team_id)
    GROUP BY u.id
    ORDER BY u.first_name, u.last_name;
END$$

DROP PROCEDURE IF EXISTS sp_GetManagersAndAdmins$$
CREATE PROCEDURE sp_GetManagersAndAdmins()
BEGIN
    SELECT u.id, CONCAT(u.first_name, ' ', u.last_name) as full_name 
    FROM users u
    JOIN roles r ON u.role_id = r.id
    WHERE u.is_active = 1 AND r.name IN ('Admin', 'Manager')
    ORDER BY u.first_name, u.last_name;
END$$

DROP PROCEDURE IF EXISTS sp_GetUserProfile$$
CREATE PROCEDURE sp_GetUserProfile(
    IN p_user_id INT
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.email, u.phone, u.employee_id, u.job_title, u.profile_image, u.hire_date,
           d.name as department_name, t.name as team_name, r.name as role_name,
           CONCAT(m.first_name, ' ', m.last_name) as manager_name,
           (SELECT COUNT(*) FROM tasks WHERE assigned_to = u.id) as total_tasks,
           (SELECT COUNT(*) FROM tasks tk JOIN statuses s ON tk.status_id = s.id WHERE tk.assigned_to = u.id AND s.name = 'completed') as completed_tasks,
           (SELECT COUNT(*) FROM tasks tk JOIN statuses s ON tk.status_id = s.id WHERE tk.assigned_to = u.id AND s.name IN ('in_progress', 'in_review', 'testing')) as in_progress_tasks,
           (SELECT COUNT(*) FROM tasks tk JOIN statuses s ON tk.status_id = s.id WHERE tk.assigned_to = u.id AND tk.due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled')) as overdue_tasks
    FROM users u
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    LEFT JOIN roles r ON u.role_id = r.id
    LEFT JOIN users m ON u.reports_to = m.id
    WHERE u.id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_VerifyUserPassword$$
CREATE PROCEDURE sp_VerifyUserPassword(
    IN p_user_id INT
)
BEGIN
    SELECT password FROM users WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_ChangeUserPassword$$
CREATE PROCEDURE sp_ChangeUserPassword(
    IN p_user_id INT,
    IN p_new_password VARCHAR(255),
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE users SET password = p_new_password, updated_at = p_updated_at WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTeamsByDepartment$$
CREATE PROCEDURE sp_GetTeamsByDepartment(
    IN p_department_id INT
)
BEGIN
    SELECT id, name FROM teams WHERE department_id = p_department_id AND is_active = 1 ORDER BY name;
END$$

DROP PROCEDURE IF EXISTS sp_GetProfileForEdit$$
CREATE PROCEDURE sp_GetProfileForEdit(
    IN p_user_id INT
)
BEGIN
    SELECT id, first_name, last_name, email, phone, job_title, profile_image
    FROM users WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateProfile$$
CREATE PROCEDURE sp_UpdateProfile(
    IN p_user_id INT,
    IN p_first_name VARCHAR(100),
    IN p_last_name VARCHAR(100),
    IN p_email VARCHAR(255),
    IN p_phone VARCHAR(20),
    IN p_job_title VARCHAR(100),
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE users 
    SET first_name = p_first_name,
        last_name = p_last_name,
        email = p_email,
        phone = p_phone,
        job_title = p_job_title,
        updated_at = p_updated_at
    WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateProfilePicture$$
CREATE PROCEDURE sp_UpdateProfilePicture(
    IN p_user_id INT,
    IN p_image_path VARCHAR(500),
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE users SET profile_image = p_image_path, updated_at = p_updated_at WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_RemoveProfilePicture$$
CREATE PROCEDURE sp_RemoveProfilePicture(
    IN p_user_id INT,
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE users SET profile_image = NULL, updated_at = p_updated_at WHERE id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetUserProfileImage$$
CREATE PROCEDURE sp_GetUserProfileImage(
    IN p_user_id INT
)
BEGIN
    SELECT profile_image FROM users WHERE id = p_user_id;
END$$

-- ============================================
-- TASK SERVICE PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_GetAllTasks$$
CREATE PROCEDURE sp_GetAllTasks()
BEGIN
    SELECT t.id, t.title, t.description, t.due_date, c.name as category_name, p.name as priority_name, s.name as status_name
    FROM tasks t
    LEFT JOIN categories c ON t.category_id = c.id
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    ORDER BY t.created_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskById$$
CREATE PROCEDURE sp_GetTaskById(
    IN p_task_id INT
)
BEGIN
    SELECT t.id, t.title, t.description, t.due_date, t.created_at, t.updated_at,
           t.created_by, t.assigned_to, t.hourly_rate,
           c.name as category_name, p.name as priority_name, s.name as status_name,
           CONCAT(u.first_name, ' ', u.last_name) as assignee_name,
           CONCAT(c_user.first_name, ' ', c_user.last_name) as creator_name
    FROM tasks t
    LEFT JOIN categories c ON t.category_id = c.id
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN users u ON t.assigned_to = u.id
    LEFT JOIN users c_user ON t.created_by = c_user.id
    WHERE t.id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_CreateTask$$
CREATE PROCEDURE sp_CreateTask(
    IN p_title VARCHAR(200),
    IN p_description TEXT,
    IN p_project_id INT,
    IN p_category_id INT,
    IN p_priority_id INT,
    IN p_status_id INT,
    IN p_due_date DATE,
    IN p_estimated_hours DECIMAL(10,2),
    IN p_hourly_rate DECIMAL(10,2),
    IN p_created_by INT,
    IN p_created_at DATETIME,
    IN p_updated_at DATETIME
)
BEGIN
    INSERT INTO tasks (title, description, project_id, category_id, priority_id, status_id, 
                      due_date, estimated_hours, hourly_rate, created_by, created_at, updated_at)
    VALUES (p_title, p_description, p_project_id, p_category_id, p_priority_id, p_status_id, 
            p_due_date, p_estimated_hours, p_hourly_rate, p_created_by, p_created_at, p_updated_at);
    
    SELECT LAST_INSERT_ID() as id;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateTask$$
CREATE PROCEDURE sp_UpdateTask(
    IN p_id INT,
    IN p_title VARCHAR(200),
    IN p_description TEXT,
    IN p_category_id INT,
    IN p_priority_id INT,
    IN p_status_id INT,
    IN p_due_date DATE,
    IN p_estimated_hours DECIMAL(10,2),
    IN p_hourly_rate DECIMAL(10,2),
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE tasks 
    SET title = p_title, description = p_description, category_id = p_category_id, 
        priority_id = p_priority_id, status_id = p_status_id, due_date = p_due_date,
        estimated_hours = p_estimated_hours, hourly_rate = p_hourly_rate, updated_at = p_updated_at
    WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS sp_DeleteTask$$
CREATE PROCEDURE sp_DeleteTask(
    IN p_task_id INT
)
BEGIN
    DELETE FROM tasks WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskCountsByStatus$$
CREATE PROCEDURE sp_GetTaskCountsByStatus()
BEGIN
    SELECT s.name as status_name, COUNT(*) as cnt
    FROM tasks t
    LEFT JOIN statuses s ON t.status_id = s.id
    GROUP BY s.id, s.name
    ORDER BY s.id;
END$$

DROP PROCEDURE IF EXISTS sp_GetOverdueTasks$$
CREATE PROCEDURE sp_GetOverdueTasks()
BEGIN
    SELECT t.id, t.title, t.due_date, c.name as category_name
    FROM tasks t
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN categories c ON t.category_id = c.id
    WHERE t.due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled')
    ORDER BY t.due_date ASC;
END$$

DROP PROCEDURE IF EXISTS sp_GetRecentTasksList$$
CREATE PROCEDURE sp_GetRecentTasksList()
BEGIN
    SELECT t.id, t.title, s.name as status_name, c.name as category_name
    FROM tasks t
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN categories c ON t.category_id = c.id
    ORDER BY t.created_at DESC
    LIMIT 10;
END$$

-- ============================================
-- TASK SERVICE EXTENDED PROCEDURES
-- ============================================

DROP PROCEDURE IF EXISTS sp_GetTasksForEmployee$$
CREATE PROCEDURE sp_GetTasksForEmployee(
    IN p_user_id INT
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
    WHERE t.assigned_to = p_user_id
    ORDER BY t.due_date ASC;
END$$

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
END$$$

DROP PROCEDURE IF EXISTS sp_GetTaskForEdit$$
CREATE PROCEDURE sp_GetTaskForEdit(
    IN p_task_id INT
)
BEGIN
    SELECT id, title, description, project_id, category_id, priority_id, status_id, 
           task_type, assigned_to, start_date, due_date, estimated_hours, is_billable, hourly_rate, tags
    FROM tasks WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_CreateTaskExtended$$
CREATE PROCEDURE sp_CreateTaskExtended(
    IN p_title VARCHAR(200),
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
    IN p_estimated_hours DECIMAL(10,2),
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

DROP PROCEDURE IF EXISTS sp_UpdateTaskExtended$$
CREATE PROCEDURE sp_UpdateTaskExtended(
    IN p_id INT,
    IN p_title VARCHAR(200),
    IN p_description TEXT,
    IN p_project_id INT,
    IN p_category_id INT,
    IN p_priority_id INT,
    IN p_status_id INT,
    IN p_task_type VARCHAR(50),
    IN p_assigned_to INT,
    IN p_start_date DATE,
    IN p_due_date DATE,
    IN p_estimated_hours DECIMAL(10,2),
    IN p_is_billable BOOLEAN,
    IN p_hourly_rate DECIMAL(10,2),
    IN p_tags TEXT,
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE tasks SET 
        title = p_title, description = p_description, project_id = p_project_id,
        category_id = p_category_id, priority_id = p_priority_id, status_id = p_status_id,
        task_type = p_task_type, assigned_to = p_assigned_to, start_date = p_start_date,
        due_date = p_due_date, estimated_hours = p_estimated_hours, is_billable = p_is_billable,
        hourly_rate = p_hourly_rate, tags = p_tags, updated_at = p_updated_at
    WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetMyTasks$$
CREATE PROCEDURE sp_GetMyTasks(
    IN p_user_id INT
)
BEGIN
    SELECT t.id, t.title, t.description, t.due_date, t.task_type, t.progress, t.status_id,
           c.name as category_name, p.name as priority_name, s.name as status_name,
           proj.name as project_name
    FROM tasks t
    LEFT JOIN categories c ON t.category_id = c.id
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN projects proj ON t.project_id = proj.id
    WHERE t.assigned_to = p_user_id
    ORDER BY 
        CASE WHEN t.due_date < CURDATE() THEN 0 ELSE 1 END,
        t.due_date ASC,
        p.level DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskDetailsEnhanced$$
CREATE PROCEDURE sp_GetTaskDetailsEnhanced(
    IN p_task_id INT
)
BEGIN
    SELECT t.id, t.title, t.description, t.due_date, t.start_date, t.completed_date, 
           t.task_type, t.progress, t.estimated_hours, t.actual_hours, t.is_billable, t.tags,
           t.created_at, t.updated_at, t.project_id, t.assigned_to, t.assigned_at, t.created_by,
           c.name as category_name, p.name as priority_name, s.name as status_name,
           CONCAT(u.first_name, ' ', u.last_name) as assignee_name, u.profile_image as assignee_image,
           CONCAT(creator.first_name, ' ', creator.last_name) as creator_name,
           proj.name as project_name, proj.code as project_code
    FROM tasks t
    LEFT JOIN categories c ON t.category_id = c.id
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    LEFT JOIN users u ON t.assigned_to = u.id
    LEFT JOIN users creator ON t.created_by = creator.id
    LEFT JOIN projects proj ON t.project_id = proj.id
    WHERE t.id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskComments$$
CREATE PROCEDURE sp_GetTaskComments(
    IN p_task_id INT
)
BEGIN
    SELECT c.id, c.task_id, c.user_id, c.comment, c.comment_type, c.is_internal, c.created_at,
           CONCAT(u.first_name, ' ', u.last_name) as user_name, u.profile_image as user_image
    FROM task_comments c
    LEFT JOIN users u ON c.user_id = u.id
    WHERE c.task_id = p_task_id
    ORDER BY c.created_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskAttachments$$
CREATE PROCEDURE sp_GetTaskAttachments(
    IN p_task_id INT
)
BEGIN
    SELECT ta.id, ta.file_name, ta.original_name, ta.file_type, ta.file_size, 
           ta.file_path, ta.created_at, CONCAT(u.first_name, ' ', u.last_name) as uploader_name
    FROM task_attachments ta
    LEFT JOIN users u ON ta.uploaded_by = u.id
    WHERE ta.task_id = p_task_id
    ORDER BY ta.created_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateTaskStatus$$
CREATE PROCEDURE sp_UpdateTaskStatus(
    IN p_task_id INT,
    IN p_status_id INT,
    IN p_completed_date DATETIME,
    IN p_updated_at DATETIME
)
BEGIN
    UPDATE tasks SET status_id = p_status_id, completed_date = p_completed_date, updated_at = p_updated_at 
    WHERE id = p_task_id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTimeLogs$$
CREATE PROCEDURE sp_GetTimeLogs(
    IN p_user_id INT
)
BEGIN
    SELECT tl.id, tl.task_id, tl.hours_logged, tl.log_date, tl.description, tl.is_billable, tl.is_approved,
           t.title as task_title
    FROM time_logs tl
    LEFT JOIN tasks t ON tl.task_id = t.id
    WHERE tl.user_id = p_user_id
    ORDER BY tl.log_date DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetActiveUsers$$
CREATE PROCEDURE sp_GetActiveUsers()
BEGIN
    SELECT id, CONCAT(first_name, ' ', last_name) as full_name 
    FROM users WHERE is_active = 1
    ORDER BY first_name, last_name;
END$$

DELIMITER ;

-- ============================================
-- VERIFICATION
-- ============================================
DELIMITER ;
SELECT 'All service stored procedures created successfully!' as Status;
SELECT 'Total procedures created: 50+' as Info;

DELIMITER $

-- Additional Task Service Procedures
DROP PROCEDURE IF EXISTS sp_GetTasksForUser$$
CREATE PROCEDURE sp_GetTasksForUser(
    IN p_user_id INT,
    IN p_status_id INT,
    IN p_priority_id INT,
    IN p_category_id INT
)
BEGIN
    SELECT t.id, t.title, t.description, t.due_date, c.name as category_name, p.name as priority_name, s.name as status_name
    FROM tasks t
    LEFT JOIN categories c ON t.category_id = c.id
    LEFT JOIN priorities p ON t.priority_id = p.id
    LEFT JOIN statuses s ON t.status_id = s.id
    WHERE (t.created_by = p_user_id OR t.assigned_to = p_user_id)
      AND (p_status_id IS NULL OR t.status_id = p_status_id)
      AND (p_priority_id IS NULL OR t.priority_id = p_priority_id)
      AND (p_category_id IS NULL OR t.category_id = p_category_id)
    ORDER BY t.created_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetCategories$$
CREATE PROCEDURE sp_GetCategories()
BEGIN
    SELECT id, name, description, color, icon, is_active 
    FROM categories WHERE is_active = 1;
END$$

DROP PROCEDURE IF EXISTS sp_GetPriorities$$
CREATE PROCEDURE sp_GetPriorities()
BEGIN
    SELECT id, name, level, color FROM priorities;
END$$

DROP PROCEDURE IF EXISTS sp_GetStatuses$$
CREATE PROCEDURE sp_GetStatuses()
BEGIN
    SELECT id, name, display_name, color, sort_order 
    FROM statuses ORDER BY sort_order;
END$$

DROP PROCEDURE IF EXISTS sp_SaveTaskAttachment$$
CREATE PROCEDURE sp_SaveTaskAttachment(
    IN p_task_id INT,
    IN p_uploaded_by INT,
    IN p_file_name VARCHAR(255),
    IN p_original_name VARCHAR(255),
    IN p_file_type VARCHAR(50),
    IN p_file_size BIGINT,
    IN p_file_path VARCHAR(500),
    IN p_created_at DATETIME
)
BEGIN
    INSERT INTO task_attachments 
    (task_id, uploaded_by, file_name, original_name, file_type, file_size, file_path, created_at) 
    VALUES (p_task_id, p_uploaded_by, p_file_name, p_original_name, p_file_type, p_file_size, p_file_path, p_created_at);
END$$

DROP PROCEDURE IF EXISTS sp_GetTaskAttachments$$
CREATE PROCEDURE sp_GetTaskAttachments(
    IN p_task_id INT
)
BEGIN
    SELECT ta.id, ta.file_name, ta.original_name, ta.file_type, ta.file_size, 
           ta.file_path, ta.created_at, CONCAT(u.first_name, ' ', u.last_name) as uploaded_by_name
    FROM task_attachments ta
    LEFT JOIN users u ON ta.uploaded_by = u.id
    WHERE ta.task_id = p_task_id
    ORDER BY ta.created_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetAttachmentFilePath$$
CREATE PROCEDURE sp_GetAttachmentFilePath(
    IN p_attachment_id INT
)
BEGIN
    SELECT file_path FROM task_attachments WHERE id = p_attachment_id;
END$$

DROP PROCEDURE IF EXISTS sp_DeleteTaskAttachment$$
CREATE PROCEDURE sp_DeleteTaskAttachment(
    IN p_attachment_id INT
)
BEGIN
    DELETE FROM task_attachments WHERE id = p_attachment_id;
END$$

DELIMITER ;


-- Pending User Service Procedures
DROP PROCEDURE IF EXISTS sp_GetPendingUsers$$
CREATE PROCEDURE sp_GetPendingUsers(
    IN p_status_filter VARCHAR(50)
)
BEGIN
    SELECT pu.id, pu.first_name, pu.last_name, pu.email, pu.phone, pu.job_title, pu.status,
           pu.requested_at, pu.reviewed_at, pu.rejection_reason,
           d.name as department_name,
           CONCAT(u.first_name, ' ', u.last_name) as reviewer_name
    FROM pending_users pu
    LEFT JOIN departments d ON pu.department_id = d.id
    LEFT JOIN users u ON pu.reviewed_by = u.id
    WHERE (p_status_filter IS NULL OR pu.status = p_status_filter)
    ORDER BY pu.requested_at DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetPendingUserById$$
CREATE PROCEDURE sp_GetPendingUserById(
    IN p_id INT
)
BEGIN
    SELECT id, first_name, last_name, email, phone, job_title, department_id
    FROM pending_users WHERE id = p_id AND status = 'pending';
END$$

DROP PROCEDURE IF EXISTS sp_GetPendingUserPassword$$
CREATE PROCEDURE sp_GetPendingUserPassword(
    IN p_id INT
)
BEGIN
    SELECT password FROM pending_users WHERE id = p_id AND status = 'pending';
END$$

DROP PROCEDURE IF EXISTS sp_ApprovePendingUser$$
CREATE PROCEDURE sp_ApprovePendingUser(
    IN p_first_name VARCHAR(100),
    IN p_last_name VARCHAR(100),
    IN p_email VARCHAR(255),
    IN p_password VARCHAR(255),
    IN p_phone VARCHAR(20),
    IN p_employee_id VARCHAR(50),
    IN p_job_title VARCHAR(100),
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_team_id INT,
    IN p_reports_to INT,
    IN p_hire_date DATE,
    IN p_created_at DATETIME,
    IN p_pending_user_id INT,
    IN p_reviewed_by INT,
    IN p_reviewed_at DATETIME
)
BEGIN
    -- Create user
    INSERT INTO users (first_name, last_name, email, password, phone, employee_id, job_title, 
                       role_id, department_id, team_id, reports_to, hire_date, 
                       is_active, is_verified, created_at)
    VALUES (p_first_name, p_last_name, p_email, p_password, p_phone, p_employee_id, p_job_title, 
            p_role_id, p_department_id, p_team_id, p_reports_to, p_hire_date, 
            1, 1, p_created_at);
    
    -- Update pending user status
    UPDATE pending_users 
    SET status = 'approved', reviewed_at = p_reviewed_at, reviewed_by = p_reviewed_by 
    WHERE id = p_pending_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_RejectPendingUser$$
CREATE PROCEDURE sp_RejectPendingUser(
    IN p_id INT,
    IN p_reviewed_at DATETIME,
    IN p_reviewed_by INT,
    IN p_rejection_reason TEXT
)
BEGIN
    UPDATE pending_users 
    SET status = 'rejected', reviewed_at = p_reviewed_at, reviewed_by = p_reviewed_by, rejection_reason = p_rejection_reason 
    WHERE id = p_id AND status = 'pending';
END$$

DROP PROCEDURE IF EXISTS sp_GetPendingUserCount$$
CREATE PROCEDURE sp_GetPendingUserCount()
BEGIN
    SELECT COUNT(*) as count FROM pending_users WHERE status = 'pending';
END$$

DROP PROCEDURE IF EXISTS sp_GetRoles$$
CREATE PROCEDURE sp_GetRoles()
BEGIN
    SELECT id, name FROM roles WHERE id != 1 ORDER BY id;
END$$

DROP PROCEDURE IF EXISTS sp_GetTeams$$
CREATE PROCEDURE sp_GetTeams()
BEGIN
    SELECT id, name FROM teams WHERE is_active = 1 ORDER BY name;
END$$

DELIMITER ;

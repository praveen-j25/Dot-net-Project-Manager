-- =====================================================
-- Stored Procedures for UserService.cs
-- Run this script against your MySQL database
-- =====================================================

DELIMITER //

-- 1. sp_GetUsers: List users with optional filters
DROP PROCEDURE IF EXISTS sp_GetUsers//
CREATE PROCEDURE sp_GetUsers(
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_team_id INT,
    IN p_is_active TINYINT
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.email, u.employee_id, u.job_title, 
           u.profile_image, u.is_active, u.last_login,
           r.name AS role_name, d.name AS department_name, t.name AS team_name,
           (SELECT COUNT(*) FROM tasks WHERE assigned_to = u.id) AS task_count
    FROM users u
    LEFT JOIN roles r ON u.role_id = r.id
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    WHERE (p_role_id IS NULL OR u.role_id = p_role_id)
      AND (p_department_id IS NULL OR u.department_id = p_department_id)
      AND (p_team_id IS NULL OR u.team_id = p_team_id)
      AND (p_is_active IS NULL OR u.is_active = p_is_active)
    ORDER BY u.first_name, u.last_name;
END//

-- 2. sp_GetUsersGroupedByTeam: Users grouped by team with manager info
DROP PROCEDURE IF EXISTS sp_GetUsersGroupedByTeam//
CREATE PROCEDURE sp_GetUsersGroupedByTeam(
    IN p_role_id INT,
    IN p_department_id INT,
    IN p_is_active TINYINT
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.email, u.employee_id, u.job_title, 
           u.profile_image, u.is_active, u.last_login, u.team_id,
           r.name AS role_name, d.name AS department_name, t.name AS team_name,
           CONCAT(m.first_name, ' ', m.last_name) AS manager_name, m.id AS manager_id,
           (SELECT COUNT(*) FROM tasks WHERE assigned_to = u.id) AS task_count
    FROM users u
    LEFT JOIN roles r ON u.role_id = r.id
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    LEFT JOIN users m ON t.team_lead_id = m.id OR d.manager_id = m.id
    WHERE (p_role_id IS NULL OR u.role_id = p_role_id)
      AND (p_department_id IS NULL OR u.department_id = p_department_id)
      AND (p_is_active IS NULL OR u.is_active = p_is_active)
    ORDER BY t.name, d.name, u.first_name, u.last_name;
END//

-- 3. sp_GetUserById: Get single user for edit form
DROP PROCEDURE IF EXISTS sp_GetUserById//
CREATE PROCEDURE sp_GetUserById(
    IN p_id INT
)
BEGIN
    SELECT id, first_name, last_name, email, phone, employee_id, job_title, 
           role_id, department_id, team_id, reports_to, hire_date, is_active
    FROM users 
    WHERE id = p_id;
END//

-- 4. sp_CheckEmailExists: Check if email already exists
DROP PROCEDURE IF EXISTS sp_CheckEmailExists//
CREATE PROCEDURE sp_CheckEmailExists(
    IN p_email VARCHAR(255)
)
BEGIN
    SELECT COUNT(*) AS email_count FROM users WHERE email = p_email;
END//

-- 5. sp_GetMaxEmployeeId: Get highest EMP### ID for auto-generation
DROP PROCEDURE IF EXISTS sp_GetMaxEmployeeId//
CREATE PROCEDURE sp_GetMaxEmployeeId()
BEGIN
    SELECT employee_id FROM users 
    WHERE employee_id LIKE 'EMP%' 
    ORDER BY CAST(SUBSTRING(employee_id, 4) AS UNSIGNED) DESC 
    LIMIT 1;
END//

-- 6. sp_CreateUser: Insert new user and return new ID
DROP PROCEDURE IF EXISTS sp_CreateUser//
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
    IN p_is_active TINYINT,
    IN p_created_at DATETIME
)
BEGIN
    INSERT INTO users (first_name, last_name, email, password, phone, employee_id, job_title, 
                       role_id, department_id, team_id, reports_to, hire_date, is_active, is_verified, created_at)
    VALUES (p_first_name, p_last_name, p_email, p_password, p_phone, p_employee_id, p_job_title, 
            p_role_id, p_department_id, p_team_id, p_reports_to, p_hire_date, p_is_active, 1, p_created_at);
    SELECT LAST_INSERT_ID() AS new_id;
END//

-- 7. sp_UpdateUser: Update user details (password optional)
DROP PROCEDURE IF EXISTS sp_UpdateUser//
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
    IN p_is_active TINYINT,
    IN p_password VARCHAR(255)
)
BEGIN
    IF p_password IS NOT NULL AND p_password != '' THEN
        UPDATE users SET 
            first_name = p_first_name, last_name = p_last_name, email = p_email, phone = p_phone,
            employee_id = p_employee_id, job_title = p_job_title, role_id = p_role_id,
            department_id = p_department_id, team_id = p_team_id, reports_to = p_reports_to,
            hire_date = p_hire_date, is_active = p_is_active, password = p_password
        WHERE id = p_id;
    ELSE
        UPDATE users SET 
            first_name = p_first_name, last_name = p_last_name, email = p_email, phone = p_phone,
            employee_id = p_employee_id, job_title = p_job_title, role_id = p_role_id,
            department_id = p_department_id, team_id = p_team_id, reports_to = p_reports_to,
            hire_date = p_hire_date, is_active = p_is_active
        WHERE id = p_id;
    END IF;
END//

-- 8. sp_SoftDeleteUser: Check email, unassign tasks, and deactivate
DROP PROCEDURE IF EXISTS sp_SoftDeleteUser//
CREATE PROCEDURE sp_SoftDeleteUser(
    IN p_id INT
)
BEGIN
    DECLARE v_email VARCHAR(255);
    
    -- Check if user exists and get email
    SELECT email INTO v_email FROM users WHERE id = p_id;
    
    -- Block deletion of system admin
    IF p_id = 1 OR v_email = 'admin@taskmanager.com' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Cannot delete system administrator';
    ELSE
        -- Unassign all tasks
        UPDATE tasks SET assigned_to = NULL WHERE assigned_to = p_id;
        -- Soft delete
        UPDATE users SET is_active = 0 WHERE id = p_id;
    END IF;
END//

-- 9. sp_GetEmployeesForAssignment: Active non-admin employees for task assignment
DROP PROCEDURE IF EXISTS sp_GetEmployeesForAssignment//
CREATE PROCEDURE sp_GetEmployeesForAssignment()
BEGIN
    SELECT id, CONCAT(first_name, ' ', last_name) AS full_name, job_title
    FROM users 
    WHERE is_active = 1 AND role_id != 1 
    ORDER BY first_name, last_name;
END//

-- 10. sp_GetEmployeeSummaries: Employee stats with optional dept/team filters
DROP PROCEDURE IF EXISTS sp_GetEmployeeSummaries//
CREATE PROCEDURE sp_GetEmployeeSummaries(
    IN p_department_id INT,
    IN p_team_id INT
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.profile_image, u.job_title,
           d.name AS department_name, t.name AS team_name,
           COUNT(DISTINCT tk.id) AS total_tasks,
           COUNT(DISTINCT CASE WHEN s.name = 'completed' THEN tk.id END) AS completed_tasks,
           COUNT(DISTINCT CASE WHEN s.name IN ('in_progress', 'in_review', 'testing') THEN tk.id END) AS in_progress_tasks,
           COUNT(DISTINCT CASE WHEN tk.due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled') THEN tk.id END) AS overdue_tasks,
           COALESCE(SUM(DISTINCT tl.hours_logged), 0) AS hours_logged
    FROM users u
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    LEFT JOIN tasks tk ON tk.assigned_to = u.id
    LEFT JOIN statuses s ON tk.status_id = s.id
    LEFT JOIN time_logs tl ON tl.user_id = u.id
    WHERE u.is_active = 1 AND u.role_id != 1
      AND (p_department_id IS NULL OR u.department_id = p_department_id)
      AND (p_team_id IS NULL OR u.team_id = p_team_id)
    GROUP BY u.id 
    ORDER BY completed_tasks DESC;
END//

-- 11. sp_GetRoles: All roles (optionally including admin)
DROP PROCEDURE IF EXISTS sp_GetRoles//
CREATE PROCEDURE sp_GetRoles(
    IN p_include_admin TINYINT
)
BEGIN
    IF p_include_admin = 1 THEN
        SELECT id, name FROM roles ORDER BY id;
    ELSE
        SELECT id, name FROM roles WHERE id != 1 ORDER BY id;
    END IF;
END//

-- 12. sp_GetDepartments: Active departments
DROP PROCEDURE IF EXISTS sp_GetDepartments//
CREATE PROCEDURE sp_GetDepartments()
BEGIN
    SELECT id, name FROM departments WHERE is_active = 1 ORDER BY name;
END//

-- 13. sp_GetTeams: Active teams
DROP PROCEDURE IF EXISTS sp_GetTeams//
CREATE PROCEDURE sp_GetTeams()
BEGIN
    SELECT id, name FROM teams WHERE is_active = 1 ORDER BY name;
END//

-- 14. sp_GetTeamsByDepartment: Teams filtered by department
DROP PROCEDURE IF EXISTS sp_GetTeamsByDepartment//
CREATE PROCEDURE sp_GetTeamsByDepartment(
    IN p_department_id INT
)
BEGIN
    SELECT id, name FROM teams 
    WHERE department_id = p_department_id AND is_active = 1 
    ORDER BY name;
END//

-- 15. sp_GetManagers: Admin and Manager users
DROP PROCEDURE IF EXISTS sp_GetManagers//
CREATE PROCEDURE sp_GetManagers()
BEGIN
    SELECT u.id, CONCAT(u.first_name, ' ', u.last_name) AS full_name 
    FROM users u
    JOIN roles r ON u.role_id = r.id
    WHERE u.is_active = 1 AND (r.name = 'Admin' OR r.name = 'Manager')
    ORDER BY u.first_name;
END//

-- 16. sp_GetUserProfile: Profile with task and time stats
DROP PROCEDURE IF EXISTS sp_GetUserProfile//
CREATE PROCEDURE sp_GetUserProfile(
    IN p_user_id INT
)
BEGIN
    SELECT u.id, u.first_name, u.last_name, u.email, u.phone, u.employee_id, 
           u.job_title, u.profile_image, u.hire_date,
           d.name AS department_name, t.name AS team_name, r.name AS role_name,
           CONCAT(m.first_name, ' ', m.last_name) AS manager_name,
           COUNT(DISTINCT tk.id) AS total_tasks,
           SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) AS completed_tasks,
           SUM(CASE WHEN tk.due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled') THEN 1 ELSE 0 END) AS overdue_tasks,
           COALESCE(SUM(tl.hours_logged), 0) AS hours_logged
    FROM users u
    LEFT JOIN departments d ON u.department_id = d.id
    LEFT JOIN teams t ON u.team_id = t.id
    LEFT JOIN roles r ON u.role_id = r.id
    LEFT JOIN users m ON u.reports_to = m.id
    LEFT JOIN tasks tk ON tk.assigned_to = u.id
    LEFT JOIN statuses s ON tk.status_id = s.id
    LEFT JOIN time_logs tl ON tl.user_id = u.id
    WHERE u.id = p_user_id
    GROUP BY u.id;
END//

-- 17. sp_GetPasswordHash: Get password hash for verification
DROP PROCEDURE IF EXISTS sp_GetPasswordHash//
CREATE PROCEDURE sp_GetPasswordHash(
    IN p_user_id INT
)
BEGIN
    SELECT password FROM users WHERE id = p_user_id AND is_active = 1;
END//

-- 18. sp_ChangePassword: Update password hash
DROP PROCEDURE IF EXISTS sp_ChangePassword//
CREATE PROCEDURE sp_ChangePassword(
    IN p_user_id INT,
    IN p_password VARCHAR(255)
)
BEGIN
    UPDATE users SET password = p_password WHERE id = p_user_id;
END//

-- 19. sp_GetProfileForEdit: Basic profile fields for editing
DROP PROCEDURE IF EXISTS sp_GetProfileForEdit//
CREATE PROCEDURE sp_GetProfileForEdit(
    IN p_user_id INT
)
BEGIN
    SELECT id, first_name, last_name, email, phone, job_title, profile_image
    FROM users WHERE id = p_user_id;
END//

-- 20. sp_UpdateProfile: Update basic profile fields
DROP PROCEDURE IF EXISTS sp_UpdateProfile//
CREATE PROCEDURE sp_UpdateProfile(
    IN p_id INT,
    IN p_first_name VARCHAR(100),
    IN p_last_name VARCHAR(100),
    IN p_email VARCHAR(255),
    IN p_phone VARCHAR(20),
    IN p_job_title VARCHAR(100)
)
BEGIN
    UPDATE users 
    SET first_name = p_first_name, last_name = p_last_name, email = p_email,
        phone = p_phone, job_title = p_job_title
    WHERE id = p_id;
END//

-- 21. sp_GetProfileImage: Get current profile image path
DROP PROCEDURE IF EXISTS sp_GetProfileImage//
CREATE PROCEDURE sp_GetProfileImage(
    IN p_user_id INT
)
BEGIN
    SELECT profile_image FROM users WHERE id = p_user_id;
END//

-- 22. sp_UpdateProfileImage: Set new profile image path
DROP PROCEDURE IF EXISTS sp_UpdateProfileImage//
CREATE PROCEDURE sp_UpdateProfileImage(
    IN p_user_id INT,
    IN p_profile_image VARCHAR(500)
)
BEGIN
    UPDATE users SET profile_image = p_profile_image WHERE id = p_user_id;
END//

-- 23. sp_RemoveProfileImage: Set profile image to NULL
DROP PROCEDURE IF EXISTS sp_RemoveProfileImage//
CREATE PROCEDURE sp_RemoveProfileImage(
    IN p_user_id INT
)
BEGIN
    UPDATE users SET profile_image = NULL WHERE id = p_user_id;
END//

DELIMITER ;

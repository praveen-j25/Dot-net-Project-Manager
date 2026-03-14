-- Fix missing PendingUserService stored procedures
USE task_manager_db;

DELIMITER $$

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

SELECT 'PendingUserService stored procedures created successfully!' as Status;

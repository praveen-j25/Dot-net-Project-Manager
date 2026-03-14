-- Fix Stored Procedure Name Conflicts
-- This fixes the conflict between admin and manager procedures

DELIMITER $$

-- Admin version - no parameters (for admin dashboard)
DROP PROCEDURE IF EXISTS sp_GetActiveProjectsCount$$
CREATE PROCEDURE sp_GetActiveProjectsCount()
BEGIN
    SELECT COUNT(*) as count FROM projects 
    WHERE is_active = 1 AND status IN ('planning', 'active');
END$$

-- Manager version - with manager_id parameter (for manager dashboard)
DROP PROCEDURE IF EXISTS sp_GetActiveProjectsCountByManager$$
CREATE PROCEDURE sp_GetActiveProjectsCountByManager(IN p_manager_id INT)
BEGIN
    SELECT COUNT(*) as count FROM projects 
    WHERE manager_id = p_manager_id AND is_active = 1 AND status IN ('planning', 'active');
END$$

DELIMITER ;

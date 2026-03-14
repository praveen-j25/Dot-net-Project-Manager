-- ============================================
-- MASTER SCRIPT TO APPLY ALL STORED PROCEDURES
-- Run this script to create all stored procedures
-- ============================================

USE task_manager_db;

-- Source the dashboard stored procedures
SOURCE dashboard_stored_procedures.sql;

-- Source all services stored procedures
SOURCE all_services_stored_procedures.sql;

-- Source existing stored procedures (if any)
SOURCE all_stored_procedures.sql;

-- Verification
SELECT 'All stored procedures applied successfully!' as Status;

-- List all stored procedures
SELECT 
    ROUTINE_NAME as 'Stored Procedure',
    ROUTINE_TYPE as 'Type',
    CREATED as 'Created Date'
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'task_manager_db'
AND ROUTINE_TYPE = 'PROCEDURE'
ORDER BY ROUTINE_NAME;

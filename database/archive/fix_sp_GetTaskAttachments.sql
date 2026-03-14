-- Fix sp_GetTaskAttachments to return uploader_name instead of uploaded_by_name
-- The C# code expects uploader_name column

USE task_manager_db;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GetTaskAttachments$$

CREATE PROCEDURE sp_GetTaskAttachments(
    IN p_task_id INT
)
BEGIN
    SELECT ta.id, ta.file_name, ta.original_name, ta.file_type, ta.file_size,
           ta.file_path, ta.created_at, 
           CONCAT(u.first_name, ' ', u.last_name) as uploader_name
    FROM task_attachments ta
    LEFT JOIN users u ON ta.uploaded_by = u.id
    WHERE ta.task_id = p_task_id
    ORDER BY ta.created_at DESC;
END$$

DELIMITER ;

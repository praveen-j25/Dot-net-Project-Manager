-- =====================================================
-- File Storage Migration: Local Files → MySQL BLOB
-- Run this script against your MySQL database
-- =====================================================

-- 1. Alter task_attachments table
ALTER TABLE task_attachments 
    ADD COLUMN file_content LONGBLOB AFTER file_size,
    ADD COLUMN content_type VARCHAR(100) DEFAULT 'application/octet-stream' AFTER file_content;

-- 2. Alter users table for profile image BLOB
ALTER TABLE users
    ADD COLUMN profile_image_content LONGBLOB AFTER profile_image,
    ADD COLUMN profile_image_type VARCHAR(100) AFTER profile_image_content;

DELIMITER //

-- 3. sp_SaveAttachment: Insert attachment with BLOB content
DROP PROCEDURE IF EXISTS sp_SaveAttachment//
CREATE PROCEDURE sp_SaveAttachment(
    IN p_task_id INT,
    IN p_uploaded_by INT,
    IN p_file_name VARCHAR(255),
    IN p_original_name VARCHAR(255),
    IN p_file_type VARCHAR(50),
    IN p_file_size BIGINT,
    IN p_file_content LONGBLOB,
    IN p_content_type VARCHAR(100),
    IN p_created_at DATETIME
)
BEGIN
    INSERT INTO task_attachments 
        (task_id, uploaded_by, file_name, original_name, file_type, file_size, file_content, content_type, created_at)
    VALUES 
        (p_task_id, p_uploaded_by, p_file_name, p_original_name, p_file_type, p_file_size, p_file_content, p_content_type, p_created_at);
    SELECT LAST_INSERT_ID() AS new_id;
END//

-- 4. sp_GetAttachments: Get attachment metadata (no BLOB) for listing
DROP PROCEDURE IF EXISTS sp_GetAttachments//
CREATE PROCEDURE sp_GetAttachments(
    IN p_task_id INT
)
BEGIN
    SELECT ta.id, ta.file_name, ta.original_name, ta.file_type, ta.file_size,
           ta.content_type, ta.created_at,
           CONCAT(u.first_name, ' ', u.last_name) AS uploaded_by_name
    FROM task_attachments ta
    LEFT JOIN users u ON ta.uploaded_by = u.id
    WHERE ta.task_id = p_task_id
    ORDER BY ta.created_at DESC;
END//

-- 5. sp_DownloadAttachment: Get BLOB content for download
DROP PROCEDURE IF EXISTS sp_DownloadAttachment//
CREATE PROCEDURE sp_DownloadAttachment(
    IN p_id INT
)
BEGIN
    SELECT file_content, content_type, original_name, file_name
    FROM task_attachments 
    WHERE id = p_id;
END//

-- 6. sp_DeleteAttachment: Delete attachment by ID
DROP PROCEDURE IF EXISTS sp_DeleteAttachment//
CREATE PROCEDURE sp_DeleteAttachment(
    IN p_id INT
)
BEGIN
    DELETE FROM task_attachments WHERE id = p_id;
END//

-- 7. sp_SaveProfileImage: Save profile image BLOB to users table
DROP PROCEDURE IF EXISTS sp_SaveProfileImage//
CREATE PROCEDURE sp_SaveProfileImage(
    IN p_user_id INT,
    IN p_profile_image_content LONGBLOB,
    IN p_profile_image_type VARCHAR(100),
    IN p_profile_image VARCHAR(500)
)
BEGIN
    UPDATE users 
    SET profile_image_content = p_profile_image_content,
        profile_image_type = p_profile_image_type,
        profile_image = p_profile_image
    WHERE id = p_user_id;
END//

-- 8. sp_GetProfileImageContent: Get profile image BLOB for serving
DROP PROCEDURE IF EXISTS sp_GetProfileImageContent//
CREATE PROCEDURE sp_GetProfileImageContent(
    IN p_user_id INT
)
BEGIN
    SELECT profile_image_content, profile_image_type 
    FROM users 
    WHERE id = p_user_id AND profile_image_content IS NOT NULL;
END//

-- 9. sp_RemoveProfileImageBlob: Clear profile image BLOB and path
DROP PROCEDURE IF EXISTS sp_RemoveProfileImageBlob//
CREATE PROCEDURE sp_RemoveProfileImageBlob(
    IN p_user_id INT
)
BEGIN
    UPDATE users 
    SET profile_image = NULL, 
        profile_image_content = NULL, 
        profile_image_type = NULL
    WHERE id = p_user_id;
END//

DELIMITER ;

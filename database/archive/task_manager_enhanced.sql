-- =====================================================
-- Enhanced Task Manager Database Schema
-- Professional IT Company Project Management System
-- Created: February 2026
-- =====================================================

USE task_manager_db;

-- =====================================================
-- TABLE: roles (Admin, Manager, Employee)
-- =====================================================
CREATE TABLE IF NOT EXISTS roles (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(255) DEFAULT NULL,
    permissions JSON DEFAULT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- TABLE: departments
-- =====================================================
CREATE TABLE IF NOT EXISTS departments (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(255) DEFAULT NULL,
    manager_id INT DEFAULT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_manager (manager_id)
);

-- =====================================================
-- TABLE: teams
-- =====================================================
CREATE TABLE IF NOT EXISTS teams (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(255) DEFAULT NULL,
    department_id INT DEFAULT NULL,
    team_lead_id INT DEFAULT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (department_id) REFERENCES departments(id) ON DELETE SET NULL,
    INDEX idx_department (department_id),
    INDEX idx_team_lead (team_lead_id)
);

-- =====================================================
-- TABLE: projects
-- =====================================================
CREATE TABLE IF NOT EXISTS projects (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(200) NOT NULL,
    description TEXT DEFAULT NULL,
    code VARCHAR(20) DEFAULT NULL,
    department_id INT DEFAULT NULL,
    manager_id INT DEFAULT NULL,
    start_date DATE DEFAULT NULL,
    end_date DATE DEFAULT NULL,
    budget DECIMAL(15,2) DEFAULT 0,
    status ENUM('planning', 'active', 'on_hold', 'completed', 'cancelled') DEFAULT 'planning',
    priority ENUM('low', 'medium', 'high', 'critical') DEFAULT 'medium',
    is_active BOOLEAN DEFAULT TRUE,
    created_by INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (department_id) REFERENCES departments(id) ON DELETE SET NULL,
    FOREIGN KEY (created_by) REFERENCES users(id),
    INDEX idx_status (status),
    INDEX idx_manager (manager_id)
);

-- =====================================================
-- ALTER users table for roles and departments
-- =====================================================
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS role_id INT DEFAULT 3,
ADD COLUMN IF NOT EXISTS department_id INT DEFAULT NULL,
ADD COLUMN IF NOT EXISTS team_id INT DEFAULT NULL,
ADD COLUMN IF NOT EXISTS job_title VARCHAR(100) DEFAULT NULL,
ADD COLUMN IF NOT EXISTS employee_id VARCHAR(50) DEFAULT NULL,
ADD COLUMN IF NOT EXISTS profile_image VARCHAR(255) DEFAULT NULL,
ADD COLUMN IF NOT EXISTS reports_to INT DEFAULT NULL,
ADD COLUMN IF NOT EXISTS hire_date DATE DEFAULT NULL,
ADD CONSTRAINT fk_user_role FOREIGN KEY (role_id) REFERENCES roles(id),
ADD CONSTRAINT fk_user_department FOREIGN KEY (department_id) REFERENCES departments(id) ON DELETE SET NULL,
ADD CONSTRAINT fk_user_team FOREIGN KEY (team_id) REFERENCES teams(id) ON DELETE SET NULL,
ADD CONSTRAINT fk_user_reports_to FOREIGN KEY (reports_to) REFERENCES users(id) ON DELETE SET NULL;

-- =====================================================
-- ALTER tasks table for project and enhanced tracking
-- =====================================================
ALTER TABLE tasks 
ADD COLUMN IF NOT EXISTS project_id INT DEFAULT NULL,
ADD COLUMN IF NOT EXISTS parent_task_id INT DEFAULT NULL,
ADD COLUMN IF NOT EXISTS task_type ENUM('task', 'bug', 'feature', 'improvement', 'support') DEFAULT 'task',
ADD COLUMN IF NOT EXISTS assigned_by INT DEFAULT NULL,
ADD COLUMN IF NOT EXISTS assigned_at TIMESTAMP NULL,
ADD COLUMN IF NOT EXISTS is_billable BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS tags VARCHAR(500) DEFAULT NULL,
ADD COLUMN IF NOT EXISTS attachments_count INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS comments_count INT DEFAULT 0,
ADD CONSTRAINT fk_task_project FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE SET NULL,
ADD CONSTRAINT fk_task_parent FOREIGN KEY (parent_task_id) REFERENCES tasks(id) ON DELETE SET NULL,
ADD CONSTRAINT fk_task_assigned_by FOREIGN KEY (assigned_by) REFERENCES users(id) ON DELETE SET NULL;

-- =====================================================
-- TABLE: task_comments (Task responses/updates)
-- =====================================================
CREATE TABLE IF NOT EXISTS task_comments (
    id INT PRIMARY KEY AUTO_INCREMENT,
    task_id INT NOT NULL,
    user_id INT NOT NULL,
    comment TEXT NOT NULL,
    comment_type ENUM('comment', 'status_update', 'progress_update', 'time_log', 'system') DEFAULT 'comment',
    is_internal BOOLEAN DEFAULT FALSE,
    parent_comment_id INT DEFAULT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (parent_comment_id) REFERENCES task_comments(id) ON DELETE SET NULL,
    INDEX idx_task (task_id),
    INDEX idx_user (user_id)
);

-- =====================================================
-- TABLE: task_activity_log (Audit trail)
-- =====================================================
CREATE TABLE IF NOT EXISTS task_activity_log (
    id INT PRIMARY KEY AUTO_INCREMENT,
    task_id INT NOT NULL,
    user_id INT NOT NULL,
    action VARCHAR(100) NOT NULL,
    old_value TEXT DEFAULT NULL,
    new_value TEXT DEFAULT NULL,
    field_changed VARCHAR(50) DEFAULT NULL,
    ip_address VARCHAR(45) DEFAULT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_task (task_id),
    INDEX idx_user (user_id),
    INDEX idx_created (created_at)
);

-- =====================================================
-- TABLE: task_attachments
-- =====================================================
CREATE TABLE IF NOT EXISTS task_attachments (
    id INT PRIMARY KEY AUTO_INCREMENT,
    task_id INT NOT NULL,
    uploaded_by INT NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    original_name VARCHAR(255) NOT NULL,
    file_type VARCHAR(100) DEFAULT NULL,
    file_size INT DEFAULT 0,
    file_path VARCHAR(500) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    FOREIGN KEY (uploaded_by) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_task (task_id)
);

-- =====================================================
-- TABLE: time_logs (Employee time tracking)
-- =====================================================
CREATE TABLE IF NOT EXISTS time_logs (
    id INT PRIMARY KEY AUTO_INCREMENT,
    task_id INT NOT NULL,
    user_id INT NOT NULL,
    project_id INT DEFAULT NULL,
    hours_logged DECIMAL(5,2) NOT NULL,
    log_date DATE NOT NULL,
    description TEXT DEFAULT NULL,
    is_billable BOOLEAN DEFAULT FALSE,
    is_approved BOOLEAN DEFAULT FALSE,
    approved_by INT DEFAULT NULL,
    approved_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE SET NULL,
    FOREIGN KEY (approved_by) REFERENCES users(id) ON DELETE SET NULL,
    INDEX idx_task (task_id),
    INDEX idx_user (user_id),
    INDEX idx_date (log_date)
);

-- =====================================================
-- TABLE: notifications
-- =====================================================
CREATE TABLE IF NOT EXISTS notifications (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    type ENUM('task_assigned', 'task_updated', 'comment_added', 'deadline_reminder', 'system', 'mention') DEFAULT 'system',
    reference_type VARCHAR(50) DEFAULT NULL,
    reference_id INT DEFAULT NULL,
    is_read BOOLEAN DEFAULT FALSE,
    read_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_user (user_id),
    INDEX idx_read (is_read),
    INDEX idx_created (created_at)
);

-- =====================================================
-- TABLE: project_members
-- =====================================================
CREATE TABLE IF NOT EXISTS project_members (
    id INT PRIMARY KEY AUTO_INCREMENT,
    project_id INT NOT NULL,
    user_id INT NOT NULL,
    role ENUM('manager', 'lead', 'member', 'viewer') DEFAULT 'member',
    joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    UNIQUE KEY uk_project_user (project_id, user_id)
);

-- =====================================================
-- TABLE: task_assignees (Multiple assignees support)
-- =====================================================
CREATE TABLE IF NOT EXISTS task_assignees (
    id INT PRIMARY KEY AUTO_INCREMENT,
    task_id INT NOT NULL,
    user_id INT NOT NULL,
    assigned_by INT NOT NULL,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (assigned_by) REFERENCES users(id) ON DELETE CASCADE,
    UNIQUE KEY uk_task_user (task_id, user_id)
);

-- =====================================================
-- DEFAULT DATA: Roles
-- =====================================================
INSERT INTO roles (name, description, permissions) VALUES
('Admin', 'Full system access, can manage all users, projects, and tasks', 
 '{"users": ["create", "read", "update", "delete"], "projects": ["create", "read", "update", "delete"], "tasks": ["create", "read", "update", "delete", "assign"], "reports": ["view", "export"], "settings": ["manage"]}'),
('Manager', 'Can manage team members, projects, and assign tasks',
 '{"users": ["read"], "projects": ["create", "read", "update"], "tasks": ["create", "read", "update", "delete", "assign"], "reports": ["view"]}'),
('Employee', 'Can view and update assigned tasks, log time',
 '{"tasks": ["read", "update_own"], "time": ["log"]}');

-- =====================================================
-- DEFAULT DATA: Departments
-- =====================================================
INSERT INTO departments (name, description) VALUES
('Engineering', 'Software Development and Engineering'),
('Design', 'UI/UX and Graphic Design'),
('Quality Assurance', 'Testing and Quality Control'),
('Human Resources', 'HR and Recruitment'),
('Marketing', 'Marketing and Sales'),
('Operations', 'Business Operations');

-- =====================================================
-- DEFAULT DATA: Teams
-- =====================================================
INSERT INTO teams (name, description, department_id) VALUES
('Backend Team', 'Server-side development', 1),
('Frontend Team', 'Client-side development', 1),
('Mobile Team', 'Mobile app development', 1),
('DevOps Team', 'Infrastructure and deployment', 1),
('UI/UX Team', 'User interface design', 2),
('QA Team', 'Quality assurance testing', 3);

-- =====================================================
-- Update statuses for professional workflow
-- =====================================================
DELETE FROM statuses;
INSERT INTO statuses (name, display_name, color, sort_order) VALUES
('backlog', 'Backlog', '#6c757d', 1),
('todo', 'To Do', '#17a2b8', 2),
('in_progress', 'In Progress', '#0d6efd', 3),
('in_review', 'In Review', '#6f42c1', 4),
('testing', 'Testing', '#fd7e14', 5),
('blocked', 'Blocked', '#dc3545', 6),
('completed', 'Completed', '#198754', 7),
('cancelled', 'Cancelled', '#adb5bd', 8);

-- =====================================================
-- Create admin user (password: Admin@123)
-- =====================================================
UPDATE users SET role_id = 3 WHERE role_id IS NULL;

INSERT INTO users (first_name, last_name, email, password, role_id, is_verified, employee_id, job_title) VALUES
('System', 'Administrator', 'admin@taskmanager.com', '$2a$11$rBHXMkvPxZfYGJGvPFqzOeUHq8qMZL.Y9kX.5ZQzQZv8Z5Z5Z5Z5Z', 1, TRUE, 'EMP001', 'System Administrator');

-- =====================================================
-- View: Employee Task Summary
-- =====================================================
CREATE OR REPLACE VIEW v_employee_task_summary AS
SELECT 
    u.id as user_id,
    u.first_name,
    u.last_name,
    u.email,
    r.name as role_name,
    d.name as department_name,
    t.name as team_name,
    COUNT(DISTINCT tk.id) as total_tasks,
    SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) as completed_tasks,
    SUM(CASE WHEN s.name = 'in_progress' THEN 1 ELSE 0 END) as in_progress_tasks,
    SUM(CASE WHEN tk.due_date < CURDATE() AND s.name NOT IN ('completed', 'cancelled') THEN 1 ELSE 0 END) as overdue_tasks
FROM users u
LEFT JOIN roles r ON u.role_id = r.id
LEFT JOIN departments d ON u.department_id = d.id
LEFT JOIN teams t ON u.team_id = t.id
LEFT JOIN tasks tk ON tk.assigned_to = u.id
LEFT JOIN statuses s ON tk.status_id = s.id
WHERE u.is_active = 1
GROUP BY u.id, u.first_name, u.last_name, u.email, r.name, d.name, t.name;

-- =====================================================
-- View: Project Summary
-- =====================================================
CREATE OR REPLACE VIEW v_project_summary AS
SELECT 
    p.id,
    p.name,
    p.code,
    p.status,
    p.priority,
    p.start_date,
    p.end_date,
    d.name as department_name,
    CONCAT(m.first_name, ' ', m.last_name) as manager_name,
    COUNT(DISTINCT t.id) as total_tasks,
    SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) as completed_tasks,
    ROUND(SUM(CASE WHEN s.name = 'completed' THEN 1 ELSE 0 END) * 100.0 / NULLIF(COUNT(t.id), 0), 1) as progress_percent
FROM projects p
LEFT JOIN departments d ON p.department_id = d.id
LEFT JOIN users m ON p.manager_id = m.id
LEFT JOIN tasks t ON t.project_id = p.id
LEFT JOIN statuses s ON t.status_id = s.id
GROUP BY p.id, p.name, p.code, p.status, p.priority, p.start_date, p.end_date, d.name, manager_name;

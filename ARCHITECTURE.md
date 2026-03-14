# Task Manager MVC - Architecture Overview

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENT LAYER                             │
├─────────────────────────────────────────────────────────────────┤
│  Web Browser (Razor Views)                                       │
│  - Bootstrap 5 UI                                                │
│  - jQuery/JavaScript                                             │
│  - Cookie Authentication                                         │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                      MIDDLEWARE LAYER                            │
├─────────────────────────────────────────────────────────────────┤
│  SecurityHeadersMiddleware   │  Authorization Middleware         │
│  - X-Frame-Options           │  - Policy Enforcement             │
│  - CSP Headers               │  - Role Checking                  │
│                              │                                   │
│  Anti-Forgery Middleware     │  Authentication Middleware        │
│  - CSRF Protection           │  - Cookie Validation              │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                      CONTROLLER LAYER                            │
├─────────────────────────────────────────────────────────────────┤
│  MVC Controllers                                                 │
│  ├─ AccountController        - Login, Profile, Auth              │
│  ├─ AdminController          - User/System Mgmt                  │
│  ├─ DashboardController      - Stats & Overview                  │
│  ├─ EmployeeController       - Task Execution                    │
│  ├─ FileController           - BLOB Delivery                     │
│  ├─ ManagerController        - Team Oversight                    │
│  └─ TasksController          - Task CRUD                         │
│                                                                   │
│  Authorization Policies:                                         │
│  - AdminOnly                                                     │
│  - ManagerOrAbove                                                │
│  - EmployeeOnly                                                  │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                       SERVICE LAYER                              │
├─────────────────────────────────────────────────────────────────┤
│  Business Logic Services                                         │
│  ├─ AuthService          - Authentication & User Management      │
│  ├─ TaskService          - Task Operations & BLOB Handling       │
│  ├─ UserService          - User Profiles & Images                │
│  ├─ ProjectService       - Project Hierarchy                     │
│  ├─ DashboardService     - Aggregated Statistics                 │
│  ├─ NotificationService  - Alerts & Messages                     │
│  └─ ActivityLogService   - Audit Trails                          │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    DATA ACCESS LAYER (ADO.NET)                   │
├─────────────────────────────────────────────────────────────────┤
│  DbConnectionFactory                                             │
│  ├─ MySqlConnection      - Connection Management                 │
│  ├─ MySqlCommand         - Parameterized Queries                 │
│  ├─ MySqlDataReader      - Efficient Data Retrieval              │
│                                                                   │
│  Key Implementation Details:                                     │
│  - Raw ADO.NET for maximum performance                           │
│  - Parameterized queries to prevent SQL Injection                │
│  - Using statements for deterministic resource disposal          │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                       DATABASE LAYER (MySQL)                     │
├─────────────────────────────────────────────────────────────────┤
│  Tables:                                                         │
│  ├─ users                - Accounts (Stored Procs Auth)          │
│  ├─ roles                - Admin, Manager, Employee              │
│  ├─ tasks                - Core Task Entities                    │
│  ├─ task_attachments     - File BLOBs (LONGBLOB)                 │
│  ├─ projects             - Project Containers                    │
│  ├─ notifications        - User Alerts                           │
│  └─ activity_logs        - Audit Records                         │
│                                                                   │
│  Stored Procedures:                                              │
│  ├─ sp_Login, sp_Register                                        │
│  ├─ sp_SaveAttachment (BLOB)                                     │
│  ├─ sp_GetProfileImageContent                                    │
│  └─ ...and many more highly optimized procedures                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔐 Security Architecture

### 1. Authentication Layer
- **Method**: Cookie-based authentication using `Microsoft.AspNetCore.Authentication.Cookies`.
- **Password Storage**: BCrypt hashing via `BCrypt.Net-Next`.
- **Session Management**: Secure, HttpOnly cookies with sliding expiration.

### 2. Authorization Layer
- **RBAC**: Strict Role-Based Access Control implemented via Policy-based authorization.
- **Policies**: 
    - `AdminOnly`: Full system access.
    - `ManagerOrAbove`: access to team management and reports.
    - `EmployeeOnly`: Access to assigned tasks and personal dashboard.

### 3. Data Protection
- **SQL Injection**: Prevented by exclusively using parameterized queries (ADO.NET).
- **XSS**: Mitigated by Razor's automatic HTML encoding and CSP headers.
- **CSRF**: Protected by Anti-Forgery Tokens on all POST requests.
- **File Uploads**: 
    - Stored as BLOBs to prevent direct file system execution attacks.
    - Validated by content type and size limits.

---

## 💾 Data Handling (BLOB Storage)

### File Attachments & Profile Images
- **Storage Strategy**: Files are stored directly in the database as `LONGBLOB`.
- **Retrieval**: Served via dedicated secure endpoints (`FileController`).
    - `/File/Attachment/{id}`: Downloads task files.
    - `/File/ProfileImage/{id}`: Serves optimized user avatars.
- **Benefits**:
    - Simplified backups (single database dump).
    - No file permission issues on deployment.
    - Transactional integrity (file saves commit with record creation).

---

## 🔧 Technology Stack

### Backend
- **Framework:** ASP.NET Core MVC (.NET 10.0)
- **Language:** C#
- **Data Access:** Pure ADO.NET (MySql.Data)

### Frontend
- **Engine:** Razor Views (.cshtml)
- **UI Libs:** Bootstrap 5, Bootstrap Icons
- **JS:** jQuery/Vanilla JS for interactions

### Database
- **Engine:** MySQL 8.0+
- **Schema:** Relational, Normalized (3NF)
- **Optimization:** Heavy use of Stored Procedures for business logic encapsulation.

---

## 🎯 Design Patterns

1. **Service Layer Pattern**: Decouples controllers from data access logic.
2. **Factory Pattern**: `DbConnectionFactory` manages database connections.
3. **MVC Pattern**: Strict separation of concerns (Model-View-Controller).
4. **Repository-ish Pattern**: Services act as repositories encapsulating specific data logic.

---

**Architecture Status: Production-Ready ✅**

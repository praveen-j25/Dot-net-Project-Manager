# Task Manager MVC - Architecture Overview

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENT LAYER                             │
├─────────────────────────────────────────────────────────────────┤
│  Web Browser (Razor Views)  │  Mobile/SPA (JWT API)             │
│  - Bootstrap 5 UI            │  - REST API Calls                 │
│  - jQuery/JavaScript         │  - JWT Token Auth                 │
│  - Cookie Authentication     │  - JSON Responses                 │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                      MIDDLEWARE LAYER                            │
├─────────────────────────────────────────────────────────────────┤
│  SecurityHeadersMiddleware   │  JwtAuthenticationMiddleware      │
│  - X-Frame-Options           │  - Token Validation               │
│  - CSP Headers               │  - Claims Extraction              │
│  - XSS Protection            │  - Bearer Token Support           │
│                              │                                   │
│  Anti-Forgery Middleware     │  Authorization Middleware         │
│  - CSRF Protection           │  - Policy Enforcement             │
│  - Token Validation          │  - Role Checking                  │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                      CONTROLLER LAYER                            │
├─────────────────────────────────────────────────────────────────┤
│  MVC Controllers             │  API Controllers                  │
│  ├─ AccountController        │  └─ AuthApiController             │
│  ├─ DashboardController      │     ├─ POST /api/auth/login       │
│  ├─ TasksController          │     ├─ POST /api/auth/register    │
│  ├─ AdminController          │     ├─ GET  /api/auth/me          │
│  ├─ EmployeeController       │     └─ POST /api/auth/validate    │
│  └─ HomeController           │                                   │
│                              │                                   │
│  Authorization Policies:     │                                   │
│  - AdminOnly                 │                                   │
│  - ManagerOrAbove            │                                   │
│  - ManageUsers               │                                   │
│  - ManageProjects            │                                   │
│  - AssignTasks               │                                   │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                       SERVICE LAYER                              │
├─────────────────────────────────────────────────────────────────┤
│  Business Logic Services                                         │
│  ├─ AuthService          - Authentication & User Management      │
│  ├─ JwtService           - JWT Token Generation & Validation     │
│  ├─ TaskService          - Task CRUD & Business Logic            │
│  ├─ UserService          - User Management                       │
│  ├─ ProjectService       - Project Management                    │
│  ├─ DashboardService     - Dashboard Statistics                  │
│  ├─ NotificationService  - Notifications                         │
│  └─ ActivityLogService   - Activity Tracking                     │
│                                                                   │
│  Security Services                                               │
│  ├─ BCrypt.Net           - Password Hashing                      │
│  └─ JWT Validation       - Token Security                        │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    DATA ACCESS LAYER (ADO.NET)                   │
├─────────────────────────────────────────────────────────────────┤
│  DbConnectionFactory                                             │
│  ├─ MySqlConnection      - Connection Management                 │
│  ├─ MySqlCommand         - Parameterized Queries                 │
│  ├─ MySqlDataReader      - Data Retrieval                        │
│  └─ Connection Pooling   - Performance Optimization              │
│                                                                   │
│  Patterns:                                                       │
│  - Repository Pattern                                            │
│  - Async/Await                                                   │
│  - Using Statements (Resource Disposal)                          │
│  - Parameterized Queries (SQL Injection Prevention)              │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                       DATABASE LAYER (MySQL)                     │
├─────────────────────────────────────────────────────────────────┤
│  Tables:                                                         │
│  ├─ users                - User accounts                         │
│  ├─ roles                - Admin, Manager, Employee              │
│  ├─ departments          - Organizational units                  │
│  ├─ teams                - Team structure                        │
│  ├─ projects             - Project management                    │
│  ├─ tasks                - Task tracking                         │
│  ├─ categories           - Task categories                       │
│  ├─ priorities           - Priority levels                       │
│  ├─ statuses             - Task statuses                         │
│  ├─ notifications        - User notifications                    │
│  └─ password_resets      - Password reset tokens                 │
│                                                                   │
│  Stored Procedures (11):                                         │
│  ├─ sp_authenticate_user                                         │
│  ├─ sp_create_user                                               │
│  ├─ sp_get_user_dashboard                                        │
│  ├─ sp_get_tasks                                                 │
│  ├─ sp_create_task                                               │
│  ├─ sp_update_task                                               │
│  ├─ sp_delete_task                                               │
│  ├─ sp_get_admin_dashboard                                       │
│  ├─ sp_get_project_stats                                         │
│  └─ sp_get_user_performance                                      │
│                                                                   │
│  Views (2):                                                      │
│  ├─ v_employee_task_summary                                      │
│  └─ v_project_summary                                            │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔐 Security Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      SECURITY LAYERS                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Layer 1: Transport Security                                     │
│  ├─ HTTPS Enforcement                                            │
│  ├─ HSTS Headers                                                 │
│  └─ Secure Cookies                                               │
│                                                                   │
│  Layer 2: Authentication                                         │
│  ├─ JWT Token Authentication (API)                               │
│  ├─ Cookie Authentication (Web)                                  │
│  ├─ BCrypt Password Hashing                                      │
│  └─ Token Expiration & Refresh                                   │
│                                                                   │
│  Layer 3: Authorization                                          │
│  ├─ Role-Based Access Control (RBAC)                             │
│  ├─ Permission-Based Policies                                    │
│  ├─ Resource-Based Authorization                                 │
│  └─ Custom Authorization Handlers                                │
│                                                                   │
│  Layer 4: Input Validation                                       │
│  ├─ Data Annotations                                             │
│  ├─ Model State Validation                                       │
│  ├─ Parameterized Queries                                        │
│  └─ Input Sanitization                                           │
│                                                                   │
│  Layer 5: Attack Prevention                                      │
│  ├─ CSRF Protection (Anti-Forgery Tokens)                        │
│  ├─ XSS Prevention (CSP Headers)                                 │
│  ├─ SQL Injection Prevention (Parameterized Queries)             │
│  ├─ Clickjacking Prevention (X-Frame-Options)                    │
│  └─ MIME Sniffing Prevention (X-Content-Type-Options)            │
│                                                                   │
│  Layer 6: Security Headers                                       │
│  ├─ Content-Security-Policy                                      │
│  ├─ X-Frame-Options: DENY                                        │
│  ├─ X-XSS-Protection: 1; mode=block                              │
│  ├─ X-Content-Type-Options: nosniff                              │
│  ├─ Referrer-Policy: strict-origin-when-cross-origin            │
│  └─ Permissions-Policy                                           │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Authentication Flow

### Cookie Authentication (Web UI)
```
User → Login Form → AccountController
                         ↓
                    AuthService.LoginAsync()
                         ↓
                    Verify Password (BCrypt)
                         ↓
                    Create Claims
                         ↓
                    Sign In (Cookie)
                         ↓
                    Redirect to Dashboard
```

### JWT Authentication (API)
```
Client → POST /api/auth/login → AuthApiController
                                      ↓
                                 AuthService.LoginAsync()
                                      ↓
                                 Verify Password (BCrypt)
                                      ↓
                                 JwtService.GenerateToken()
                                      ↓
                                 Return JWT Token
                                      ↓
Client Stores Token → Use in Authorization Header
                                      ↓
                      Bearer {token} → JwtAuthenticationMiddleware
                                      ↓
                                 Validate Token
                                      ↓
                                 Extract Claims
                                      ↓
                                 Set User Principal
                                      ↓
                                 Access Protected Resource
```

---

## 🎯 Authorization Flow

```
Request → Controller Action
              ↓
         [Authorize(Policy = "PolicyName")]
              ↓
         Authorization Middleware
              ↓
         Check if Authenticated
              ↓
         Evaluate Policy Requirements
              ↓
    ┌─────────┴─────────┐
    ↓                   ↓
Role Check         Permission Check
    ↓                   ↓
RoleRequirementHandler  PermissionRequirementHandler
    ↓                   ↓
Check User Role    Check User Permissions
    ↓                   ↓
    └─────────┬─────────┘
              ↓
         Success/Failure
              ↓
    ┌─────────┴─────────┐
    ↓                   ↓
Allow Access       Return 403 Forbidden
```

---

## 📊 Data Flow

### Task Creation Flow
```
User Input → TasksController.Create()
                    ↓
              Validate Model
                    ↓
              TaskService.CreateTaskAsync()
                    ↓
              DbConnectionFactory.CreateConnection()
                    ↓
              MySqlCommand (Parameterized)
                    ↓
              sp_create_task (Stored Procedure)
                    ↓
              Insert into tasks table
                    ↓
              Create notification
                    ↓
              Return task ID
                    ↓
              Redirect to Task Details
```

### Dashboard Statistics Flow
```
User Request → DashboardController.Index()
                    ↓
              Get User ID from Claims
                    ↓
              DashboardService.GetDashboardAsync()
                    ↓
              sp_get_user_dashboard (Stored Procedure)
                    ↓
              Aggregate Statistics:
              - Total Tasks
              - Completed Tasks
              - In Progress Tasks
              - Overdue Tasks
              - High Priority Tasks
                    ↓
              Return DashboardVM
                    ↓
              Render Dashboard View
```

---

## 🗂️ Project Structure

```
TaskManagerMVC/
├── Controllers/
│   ├── AccountController.cs
│   ├── AdminController.cs
│   ├── DashboardController.cs
│   ├── EmployeeController.cs
│   ├── HomeController.cs
│   ├── TasksController.cs
│   └── Api/
│       └── AuthApiController.cs
│
├── Services/
│   ├── AuthService.cs
│   ├── JwtService.cs
│   ├── TaskService.cs
│   ├── UserService.cs
│   ├── ProjectService.cs
│   ├── DashboardService.cs
│   ├── NotificationService.cs
│   └── ActivityLogService.cs
│
├── Models/
│   ├── User.cs
│   ├── TaskItem.cs
│   ├── Project.cs
│   ├── Role.cs
│   └── Lookups.cs
│
├── ViewModels/
│   ├── AccountVM.cs
│   ├── TaskVM.cs
│   └── AdminVM.cs
│
├── Views/
│   ├── Account/
│   ├── Dashboard/
│   ├── Tasks/
│   ├── Admin/
│   ├── Employee/
│   └── Shared/
│
├── Data/
│   └── AppDbContext.cs (DbConnectionFactory)
│
├── Authorization/
│   └── RoleRequirement.cs
│
├── Middleware/
│   └── JwtAuthenticationMiddleware.cs
│
├── Security/
│   └── SecurityHeadersMiddleware.cs
│
├── Filters/
│   └── ValidateAntiForgeryTokenAttribute.cs
│
└── wwwroot/
    ├── css/
    ├── js/
    └── lib/
```

---

## 🔧 Technology Stack

### Backend
- **Framework:** ASP.NET Core MVC 7.0
- **Language:** C# 11
- **Data Access:** ADO.NET (MySql.Data)
- **Authentication:** JWT + Cookie-based
- **Password Hashing:** BCrypt.Net

### Frontend
- **View Engine:** Razor
- **CSS Framework:** Bootstrap 5
- **JavaScript:** jQuery
- **Icons:** Bootstrap Icons

### Database
- **RDBMS:** MySQL 8.0+
- **ORM:** None (Pure ADO.NET)
- **Stored Procedures:** 11 procedures
- **Views:** 2 views

### Security
- **Authentication:** JWT Bearer + Cookie
- **Authorization:** Policy-based
- **CSRF:** Anti-Forgery Tokens
- **Headers:** Custom Security Middleware

---

## 📈 Performance Considerations

1. **Connection Pooling:** MySqlConnection pooling enabled
2. **Async/Await:** All database operations are async
3. **Stored Procedures:** Complex queries optimized
4. **Indexes:** Database indexes on frequently queried columns
5. **Caching:** Session-based caching for user data
6. **Lazy Loading:** Views load data on demand

---

## 🛡️ Security Best Practices Implemented

✅ **OWASP Top 10 Protection:**
1. Injection - Parameterized queries
2. Broken Authentication - JWT + BCrypt
3. Sensitive Data Exposure - HTTPS + Secure cookies
4. XML External Entities - Not applicable
5. Broken Access Control - Authorization policies
6. Security Misconfiguration - Security headers
7. XSS - CSP headers + output encoding
8. Insecure Deserialization - Input validation
9. Using Components with Known Vulnerabilities - Updated packages
10. Insufficient Logging & Monitoring - Activity logs

---

## 🎯 Design Patterns Used

1. **Repository Pattern** - Data access abstraction
2. **Service Layer Pattern** - Business logic separation
3. **Factory Pattern** - DbConnectionFactory
4. **Dependency Injection** - Constructor injection
5. **MVC Pattern** - Model-View-Controller
6. **Middleware Pattern** - Request pipeline
7. **Strategy Pattern** - Authorization handlers

---

**Architecture Status: Production-Ready ✅**

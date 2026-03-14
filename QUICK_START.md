# Quick Start Guide - Task Manager MVC

## 🚀 Setup in 5 Minutes

### Step 1: Install Dependencies
```bash
cd TaskManagerMVC
dotnet restore
```

### Step 2: Setup Database
```bash
# Login to MySQL
mysql -u root -p

# Run these commands in MySQL:
source ../database/COMPLETE_DATABASE_SETUP.sql
source ../database/stored_procedures.sql
exit
```

### Step 3: Configure Application
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=task_manager_db;User=root;Password=YOUR_PASSWORD;"
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyForJWTTokenGeneration123!@#MustBeAtLeast32CharactersLong"
  }
}
```

### Step 4: Run Application
```bash
dotnet run
```

### Step 5: Test
Open browser: https://localhost:5001

**Default Login:**
- Email: `admin@taskmanager.com`
- Password: `Admin@123`

---

## 🧪 Testing JWT API

### 1. Get JWT Token
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"admin@taskmanager.com\",\"password\":\"Admin@123\"}" \
  -k
```

**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "...",
    "tokenType": "Bearer",
    "expiresIn": 86400,
    "user": {
      "id": 1,
      "firstName": "System",
      "lastName": "Administrator",
      "email": "admin@taskmanager.com",
      "role": "Admin"
    }
  }
}
```

### 2. Use Token to Access Protected Endpoint
```bash
curl -X GET https://localhost:5001/api/auth/me \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -k
```

### 3. Validate Token
```bash
curl -X POST https://localhost:5001/api/auth/validate \
  -H "Content-Type: application/json" \
  -d "{\"token\":\"YOUR_TOKEN_HERE\"}" \
  -k
```

---

## 🧪 Testing Stored Procedures

```sql
-- Test authentication
CALL sp_authenticate_user('admin@taskmanager.com');

-- Test dashboard stats
CALL sp_get_user_dashboard(1);

-- Test task retrieval (userId, roleId, statusId, priorityId, categoryId, projectId)
CALL sp_get_tasks(1, 1, NULL, NULL, NULL, NULL);

-- Test admin dashboard
CALL sp_get_admin_dashboard();

-- Test project stats
CALL sp_get_project_stats(1);

-- Test user performance
CALL sp_get_user_performance(1, '2024-01-01', '2024-12-31');
```

---

## 🔐 Testing Authorization

### Test as Admin (Full Access)
1. Login as admin@taskmanager.com
2. Access: Dashboard, Tasks, Users, Projects, Reports
3. All features should be accessible

### Test as Manager (Limited Access)
1. Create a manager account
2. Login as manager
3. Access: Dashboard, Tasks, Team Management
4. Cannot access: User Management, System Settings

### Test as Employee (Own Tasks Only)
1. Create an employee account
2. Login as employee
3. Access: Dashboard, Own Tasks
4. Cannot access: Other users' tasks, Admin features

---

## 🛡️ Testing Security Features

### 1. CSRF Protection
Try submitting a form without anti-forgery token:
```bash
curl -X POST https://localhost:5001/Account/Login \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "email=test@test.com&password=test" \
  -k
```
**Expected:** 400 Bad Request (Invalid anti-forgery token)

### 2. Security Headers
Check response headers:
```bash
curl -I https://localhost:5001 -k
```
**Expected Headers:**
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Content-Security-Policy: ...
- Strict-Transport-Security: ...

### 3. SQL Injection Prevention
Try SQL injection in login:
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"admin' OR '1'='1\",\"password\":\"anything\"}" \
  -k
```
**Expected:** 401 Unauthorized (Invalid credentials)

### 4. JWT Token Validation
Try accessing protected endpoint with invalid token:
```bash
curl -X GET https://localhost:5001/api/auth/me \
  -H "Authorization: Bearer invalid_token" \
  -k
```
**Expected:** 401 Unauthorized

---

## 📊 Testing CRUD Operations

### Tasks CRUD
1. **Create:** Go to Tasks > Add New Task
2. **Read:** View task list and details
3. **Update:** Edit existing task
4. **Delete:** Delete a task (Admin/Manager only)

### Users CRUD (Admin Only)
1. **Create:** Admin > Users > Create User
2. **Read:** View user list
3. **Update:** Edit user details
4. **Delete:** Deactivate user

### Projects CRUD (Admin/Manager)
1. **Create:** Admin > Projects > Create Project
2. **Read:** View project list
3. **Update:** Edit project
4. **Delete:** Archive project

---

## 🐛 Troubleshooting

### Database Connection Failed
- Check MySQL is running: `mysql -u root -p`
- Verify connection string in appsettings.json
- Check user permissions

### JWT Token Invalid
- Verify SecretKey is at least 32 characters
- Check token expiration
- Ensure Issuer and Audience match

### Authorization Failed
- Check user role in database
- Verify policy configuration in Program.cs
- Check [Authorize] attributes on controllers

### Anti-Forgery Token Error
- Ensure @Html.AntiForgeryToken() in forms
- Check cookie settings
- Verify middleware order in Program.cs

---

## 📝 Next Steps

1. ✅ Test all features
2. ✅ Verify security headers
3. ✅ Test authorization policies
4. ✅ Test stored procedures
5. ✅ Test JWT authentication
6. 🚀 Deploy to production

---

## 🎯 Production Checklist

Before deploying to production:

- [ ] Change JWT SecretKey to a strong random value
- [ ] Update database connection string
- [ ] Enable HTTPS only
- [ ] Set secure cookie policies
- [ ] Configure CORS properly
- [ ] Set up logging
- [ ] Configure email service for password reset
- [ ] Set up backup strategy
- [ ] Configure rate limiting
- [ ] Set up monitoring

---

## 📚 Additional Resources

- Full Implementation Guide: `IMPLEMENTATION_GUIDE.md`
- Verification Report: `IMPLEMENTATION_VERIFICATION.md`
- Database Schema: `../database/`
- API Documentation: See AuthApiController.cs

---

**All 8 Requirements Implemented! Ready for Testing! 🎉**

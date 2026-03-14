using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskManagerMVC.Data;
using TaskManagerMVC.Models;
using TaskManagerMVC.ViewModels;
using System.Data;

namespace TaskManagerMVC.Services;

public class UserService
{
    private readonly DbConnectionFactory _dbFactory;

    public UserService(DbConnectionFactory dbFactory) => _dbFactory = dbFactory;

    /// <summary>
    /// Get all users with filters
    /// </summary>
    public async Task<UserListVM> GetUsersAsync(int? roleId = null, int? departmentId = null, int? teamId = null, bool? isActive = null)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetUsers", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_role_id", (object?)roleId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_department_id", (object?)departmentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_team_id", (object?)teamId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_is_active", isActive.HasValue ? (object)(isActive.Value ? 1 : 0) : DBNull.Value);

        var users = new List<UserItemVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new UserItemVM
            {
                Id = Convert.ToInt32(reader["id"]),
                FullName = $"{reader["first_name"].ToString()!} {reader["last_name"].ToString()!}",
                Email = reader["email"].ToString()!,
                EmployeeId = reader["employee_id"] == DBNull.Value ? null : reader["employee_id"].ToString()!,
                JobTitle = reader["job_title"] == DBNull.Value ? null : reader["job_title"].ToString()!,
                ProfileImage = reader["profile_image"] == DBNull.Value ? null : reader["profile_image"].ToString()!,
                RoleName = reader["role_name"] == DBNull.Value ? null : reader["role_name"].ToString()!,
                DepartmentName = reader["department_name"] == DBNull.Value ? null : reader["department_name"].ToString()!,
                TeamName = reader["team_name"] == DBNull.Value ? null : reader["team_name"].ToString()!,
                IsActive = Convert.ToBoolean(reader["is_active"]),
                LastLogin = reader["last_login"] == DBNull.Value ? null : Convert.ToDateTime(reader["last_login"]),
                TaskCount = Convert.ToInt32(reader["task_count"])
            });
        }

        return new UserListVM
        {
            Users = users,
            RoleId = roleId,
            DepartmentId = departmentId,
            TeamId = teamId,
            IsActive = isActive,
            Roles = new SelectList(await GetRolesAsync(true), "Id", "Name"), // Allow filtering by any role in list
            Departments = new SelectList(await GetDepartmentsAsync(), "Id", "Name"),
            Teams = new SelectList(await GetTeamsAsync(), "Id", "Name")
        };
    }

    /// <summary>
    /// Get users grouped by teams with their managers
    /// </summary>
    public async Task<UserListVM> GetUsersGroupedByTeamAsync(int? roleId = null, int? departmentId = null, bool? isActive = null)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetUsersGroupedByTeam", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_role_id", (object?)roleId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_department_id", (object?)departmentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_is_active", isActive.HasValue ? (object)(isActive.Value ? 1 : 0) : DBNull.Value);

        var teamGroups = new Dictionary<string, TeamGroupVM>();
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var teamId = reader["team_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["team_id"]);
            var teamName = reader["team_name"]?.ToString() ?? "No Team";
            var deptName = reader["department_name"]?.ToString();
            var managerName = reader["manager_name"]?.ToString();
            var managerId = reader["manager_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["manager_id"]);
            
            var groupKey = $"{teamId}_{teamName}_{deptName}";
            
            if (!teamGroups.ContainsKey(groupKey))
            {
                teamGroups[groupKey] = new TeamGroupVM
                {
                    TeamId = teamId,
                    TeamName = teamName,
                    DepartmentName = deptName,
                    ManagerName = managerName,
                    ManagerId = managerId,
                    Members = new List<UserItemVM>()
                };
            }

            teamGroups[groupKey].Members.Add(new UserItemVM
            {
                Id = Convert.ToInt32(reader["id"]),
                FullName = $"{reader["first_name"].ToString()!} {reader["last_name"].ToString()!}",
                Email = reader["email"].ToString()!,
                EmployeeId = reader["employee_id"] == DBNull.Value ? null : reader["employee_id"].ToString()!,
                JobTitle = reader["job_title"] == DBNull.Value ? null : reader["job_title"].ToString()!,
                ProfileImage = reader["profile_image"] == DBNull.Value ? null : reader["profile_image"].ToString()!,
                RoleName = reader["role_name"] == DBNull.Value ? null : reader["role_name"].ToString()!,
                DepartmentName = deptName,
                TeamName = teamName,
                IsActive = Convert.ToBoolean(reader["is_active"]),
                LastLogin = reader["last_login"] == DBNull.Value ? null : Convert.ToDateTime(reader["last_login"]),
                TaskCount = Convert.ToInt32(reader["task_count"])
            });
        }

        return new UserListVM
        {
            TeamGroups = teamGroups.Values.OrderBy(g => g.TeamName).ToList(),
            GroupByTeam = true,
            RoleId = roleId,
            DepartmentId = departmentId,
            IsActive = isActive,
            Roles = new SelectList(await GetRolesAsync(true), "Id", "Name"), // Allow filtering by any role
            Departments = new SelectList(await GetDepartmentsAsync(), "Id", "Name"),
            Teams = new SelectList(await GetTeamsAsync(), "Id", "Name")
        };
    }

    /// <summary>
    /// Get user form for create/edit
    /// </summary>
    public async Task<UserFormVM> GetUserFormAsync(int currentUserId, int? id = null)
    {
        // Only Super Admin (ID 1) can see the 'Admin' role option
        bool includeAdmin = currentUserId == 1;

        var vm = new UserFormVM
        {
            Roles = new SelectList(await GetRolesAsync(includeAdmin), "Id", "Name"),
            Departments = new SelectList(await GetDepartmentsAsync(), "Id", "Name"),
            Teams = new SelectList(await GetTeamsAsync(), "Id", "Name"),
            Managers = new SelectList(await GetManagersAsync(), "Id", "FullName")
        };

        if (id.HasValue)
        {
            using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("sp_GetUserById", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_id", id.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                vm.Id = Convert.ToInt32(reader["id"]);
                vm.FirstName = reader["first_name"].ToString()!;
                vm.LastName = reader["last_name"].ToString()!;
                vm.Email = reader["email"].ToString()!;
                vm.Phone = reader["phone"] == DBNull.Value ? null : reader["phone"].ToString()!;
                vm.EmployeeId = reader["employee_id"] == DBNull.Value ? null : reader["employee_id"].ToString()!;
                vm.JobTitle = reader["job_title"] == DBNull.Value ? null : reader["job_title"].ToString()!;
                vm.RoleId = reader["role_id"] == DBNull.Value ? 3 : Convert.ToInt32(reader["role_id"]);
                vm.DepartmentId = reader["department_id"] == DBNull.Value ? null : Convert.ToInt32(reader["department_id"]);
                vm.TeamId = reader["team_id"] == DBNull.Value ? null : Convert.ToInt32(reader["team_id"]);
                vm.ReportsTo = reader["reports_to"] == DBNull.Value ? null : Convert.ToInt32(reader["reports_to"]);
                vm.HireDate = reader["hire_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["hire_date"]);
                vm.IsActive = Convert.ToBoolean(reader["is_active"]);
            }
        }

        return vm;
    }

    /// <summary>
    /// Create new user
    /// </summary>
    public async Task<int> CreateUserAsync(UserFormVM model)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        // Check for duplicate email
        using var checkCmd = new MySqlCommand("sp_CheckEmailExists", conn);
        checkCmd.CommandType = CommandType.StoredProcedure;
        checkCmd.Parameters.AddWithValue("p_email", model.Email);
        using var checkReader = await checkCmd.ExecuteReaderAsync();
        if (await checkReader.ReadAsync() && Convert.ToInt32(checkReader["email_count"]) > 0)
            throw new InvalidOperationException($"A user with email '{model.Email}' already exists.");
        await checkReader.CloseAsync();

        // Auto-generate Employee ID if not provided
        if (string.IsNullOrEmpty(model.EmployeeId))
        {
            model.EmployeeId = await GenerateEmployeeIdAsync(conn);
        }

        using var cmd = new MySqlCommand("sp_CreateUser", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_first_name", model.FirstName);
        cmd.Parameters.AddWithValue("p_last_name", model.LastName);
        cmd.Parameters.AddWithValue("p_email", model.Email);
        cmd.Parameters.AddWithValue("p_password", BCrypt.Net.BCrypt.HashPassword(model.Password ?? "Welcome@123"));
        cmd.Parameters.AddWithValue("p_phone", (object?)model.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_employee_id", model.EmployeeId);
        cmd.Parameters.AddWithValue("p_job_title", (object?)model.JobTitle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_role_id", model.RoleId);
        cmd.Parameters.AddWithValue("p_department_id", (object?)model.DepartmentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_team_id", (object?)model.TeamId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_reports_to", (object?)model.ReportsTo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_hire_date", (object?)model.HireDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_is_active", model.IsActive);
        cmd.Parameters.AddWithValue("p_created_at", DateTime.Now);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return Convert.ToInt32(reader["new_id"]);
        return 0;
    }

    /// <summary>
    /// Generate unique Employee ID
    /// </summary>
    private async Task<string> GenerateEmployeeIdAsync(MySqlConnection conn)
    {
        using var cmd = new MySqlCommand("sp_GetMaxEmployeeId", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        using var reader = await cmd.ExecuteReaderAsync();
        int nextNumber = 1;
        if (await reader.ReadAsync())
        {
            var lastId = reader["employee_id"]?.ToString();
            if (lastId != null && lastId.StartsWith("EMP"))
            {
                var numberPart = lastId.Substring(3);
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }
        }
        await reader.CloseAsync();
        
        return $"EMP{nextNumber:D3}"; // EMP001, EMP002, etc.
    }

    /// <summary>
    /// Generate next Employee ID (public method for API)
    /// </summary>
    public async Task<string> GenerateNextEmployeeIdAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        return await GenerateEmployeeIdAsync(conn);
    }

    /// <summary>
    /// Update existing user
    /// </summary>
    public async Task UpdateUserAsync(UserFormVM model)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_UpdateUser", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_id", model.Id);
        cmd.Parameters.AddWithValue("p_first_name", model.FirstName);
        cmd.Parameters.AddWithValue("p_last_name", model.LastName);
        cmd.Parameters.AddWithValue("p_email", model.Email);
        cmd.Parameters.AddWithValue("p_phone", (object?)model.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_employee_id", (object?)model.EmployeeId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_job_title", (object?)model.JobTitle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_role_id", model.RoleId);
        cmd.Parameters.AddWithValue("p_department_id", (object?)model.DepartmentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_team_id", (object?)model.TeamId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_reports_to", (object?)model.ReportsTo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_hire_date", (object?)model.HireDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_is_active", model.IsActive);
        cmd.Parameters.AddWithValue("p_password", 
            !string.IsNullOrEmpty(model.Password) 
                ? BCrypt.Net.BCrypt.HashPassword(model.Password) 
                : (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Delete user (soft delete by setting is_active = false)
    /// </summary>
    public async Task DeleteUserAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        // SECURITY: Prevent deleting admin
        if (id == 1)
            throw new InvalidOperationException("Cannot delete system administrator");

        using var cmd = new MySqlCommand("sp_SoftDeleteUser", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Get all employees for task assignment (excludes admin)
    /// </summary>
    public async Task<List<(int Id, string FullName, string? JobTitle)>> GetEmployeesForAssignmentAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetEmployeesForAssignment", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        var employees = new List<(int, string, string?)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            employees.Add((
                Convert.ToInt32(reader["id"]),
                reader["full_name"].ToString()!,
                reader["job_title"] == DBNull.Value ? null : reader["job_title"].ToString()!
            ));
        }
        return employees;
    }

    /// <summary>
    /// Get employee summary for reports (excludes admin)
    /// </summary>
    public async Task<List<EmployeeSummaryVM>> GetEmployeeSummariesAsync(int? departmentId = null, int? teamId = null)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetEmployeeSummaries", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_department_id", (object?)departmentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_team_id", (object?)teamId ?? DBNull.Value);

        var summaries = new List<EmployeeSummaryVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            summaries.Add(new EmployeeSummaryVM
            {
                Id = Convert.ToInt32(reader["id"]),
                FullName = $"{reader["first_name"].ToString()!} {reader["last_name"].ToString()!}",
                ProfileImage = reader["profile_image"] == DBNull.Value ? null : reader["profile_image"].ToString()!,
                JobTitle = reader["job_title"] == DBNull.Value ? null : reader["job_title"].ToString()!,
                DepartmentName = reader["department_name"] == DBNull.Value ? null : reader["department_name"].ToString()!,
                TeamName = reader["team_name"] == DBNull.Value ? null : reader["team_name"].ToString()!,
                TotalTasks = Convert.ToInt32(reader["total_tasks"]),
                CompletedTasks = Convert.ToInt32(reader["completed_tasks"]),
                InProgressTasks = Convert.ToInt32(reader["in_progress_tasks"]),
                OverdueTasks = Convert.ToInt32(reader["overdue_tasks"]),
                HoursLogged = Convert.ToDecimal(reader["hours_logged"])
            });
        }

        return summaries;
    }

    // Helper methods
    private async Task<List<Role>> GetRolesAsync(bool includeAdmin = false)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetRoles", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_include_admin", includeAdmin ? 1 : 0);
        
        var roles = new List<Role>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            roles.Add(new Role { Id = Convert.ToInt32(reader["id"]), Name = reader["name"].ToString()! });
        return roles;
    }

    private async Task<List<Department>> GetDepartmentsAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetDepartments", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        
        var departments = new List<Department>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            departments.Add(new Department { Id = Convert.ToInt32(reader["id"]), Name = reader["name"].ToString()! });
        return departments;
    }

    private async Task<List<Team>> GetTeamsAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetTeams", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        
        var teams = new List<Team>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            teams.Add(new Team { Id = Convert.ToInt32(reader["id"]), Name = reader["name"].ToString()! });
        return teams;
    }

    private async Task<List<UserItemVM>> GetManagersAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetManagers", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        
        var managers = new List<UserItemVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            managers.Add(new UserItemVM 
            { 
                Id = Convert.ToInt32(reader["id"]), 
                FullName = reader["full_name"].ToString()! 
            });
        }
        return managers;
    }

    /// <summary>
    /// Get user profile with stats
    /// </summary>
    public async Task<ProfileVM> GetProfileAsync(int userId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetUserProfile", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", userId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ProfileVM
            {
                Id = Convert.ToInt32(reader["id"]),
                FullName = $"{reader["first_name"]} {reader["last_name"]}",
                Email = reader["email"].ToString()!,
                Phone = reader["phone"] == DBNull.Value ? null : reader["phone"].ToString(),
                EmployeeId = reader["employee_id"] == DBNull.Value ? null : reader["employee_id"].ToString(),
                JobTitle = reader["job_title"] == DBNull.Value ? null : reader["job_title"].ToString(),
                ProfileImage = reader["profile_image"] == DBNull.Value ? null : reader["profile_image"].ToString(),
                HireDate = reader["hire_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["hire_date"]),
                DepartmentName = reader["department_name"] == DBNull.Value ? null : reader["department_name"].ToString(),
                TeamName = reader["team_name"] == DBNull.Value ? null : reader["team_name"].ToString(),
                RoleName = reader["role_name"] == DBNull.Value ? null : reader["role_name"].ToString(),
                ManagerName = reader["manager_name"] == DBNull.Value ? null : reader["manager_name"].ToString(),
                TotalTasks = Convert.ToInt32(reader["total_tasks"]),
                CompletedTasks = Convert.ToInt32(reader["completed_tasks"]),
                PendingTasks = Convert.ToInt32(reader["total_tasks"]) - Convert.ToInt32(reader["completed_tasks"]),
                TotalHoursLogged = Convert.ToDecimal(reader["hours_logged"])
            };
        }

        return new ProfileVM();
    }

    /// <summary>
    /// Verify user's current password
    /// </summary>
    public async Task<bool> VerifyPasswordAsync(int userId, string password)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetPasswordHash", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", userId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return false;

        var storedHash = reader["password"];
        if (storedHash == null || storedHash == DBNull.Value)
            return false;

        return BCrypt.Net.BCrypt.Verify(password, storedHash.ToString()!);
    }

    /// <summary>
    /// Change user password with current password verification
    /// </summary>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        // First verify current password
        if (!await VerifyPasswordAsync(userId, currentPassword))
        {
            return (false, "Current password is incorrect");
        }

        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_ChangePassword", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_password", BCrypt.Net.BCrypt.HashPassword(newPassword));

        await cmd.ExecuteNonQueryAsync();
        return (true, "Password updated successfully");
    }

    /// <summary>
    /// Get teams filtered by department
    /// </summary>
    public async Task<List<Team>> GetTeamsByDepartmentAsync(int departmentId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetTeamsByDepartment", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_department_id", departmentId);

        var teams = new List<Team>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            teams.Add(new Team { Id = Convert.ToInt32(reader["id"]), Name = reader["name"].ToString()! });
        return teams;
    }

    public async Task<(bool Success, string Message, string? ImagePath)> UpdateProfilePictureAsync(int userId, IFormFile file)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
                return (false, "No file uploaded", null);

            if (file.Length > 5 * 1024 * 1024) // 5MB limit
                return (false, "File size must be less than 5MB", null);

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return (false, "Only image files (jpg, jpeg, png, gif) are allowed", null);

            // Read file into byte array
            byte[] fileContent;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileContent = ms.ToArray();
            }

            // Save to database
            using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new MySqlCommand("sp_SaveProfileImage", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_user_id", userId);
            cmd.Parameters.Add("p_profile_image_content", MySqlDbType.LongBlob).Value = fileContent;
            cmd.Parameters.AddWithValue("p_profile_image_type", file.ContentType ?? "image/jpeg");
            cmd.Parameters.AddWithValue("p_profile_image", $"/File/ProfileImage/{userId}");
            await cmd.ExecuteNonQueryAsync();

            return (true, "Profile picture updated successfully", $"/File/ProfileImage/{userId}");
        }
        catch (Exception ex)
        {
            return (false, $"Error uploading profile picture: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> RemoveProfilePictureAsync(int userId)
    {
        try
        {
            using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new MySqlCommand("sp_RemoveProfileImageBlob", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_user_id", userId);
            await cmd.ExecuteNonQueryAsync();

            return (true, "Profile picture removed successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error removing profile picture: {ex.Message}");
        }
    }

    public async Task<(byte[]? Content, string ContentType)?> GetProfileImageContentAsync(int userId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetProfileImageContent", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", userId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var content = reader["profile_image_content"] == DBNull.Value ? null : (byte[])reader["profile_image_content"];
        var contentType = reader["profile_image_type"]?.ToString() ?? "image/jpeg";

        return (content, contentType);
    }

    public async Task<ProfileEditVM?> GetProfileForEditAsync(int userId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetProfileForEdit", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", userId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new ProfileEditVM
        {
            Id = Convert.ToInt32(reader["id"]),
            FirstName = reader["first_name"].ToString()!,
            LastName = reader["last_name"].ToString()!,
            Email = reader["email"].ToString()!,
            Phone = reader["phone"] == DBNull.Value ? null : reader["phone"].ToString(),
            JobTitle = reader["job_title"] == DBNull.Value ? null : reader["job_title"].ToString(),
            ProfileImage = reader["profile_image"] == DBNull.Value ? null : reader["profile_image"].ToString()
        };
    }

    public async Task<(bool Success, string Message)> UpdateProfileAsync(ProfileEditVM model)
    {
        try
        {
            using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("sp_UpdateProfile", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_id", model.Id);
            cmd.Parameters.AddWithValue("p_first_name", model.FirstName);
            cmd.Parameters.AddWithValue("p_last_name", model.LastName);
            cmd.Parameters.AddWithValue("p_email", model.Email);
            cmd.Parameters.AddWithValue("p_phone", model.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("p_job_title", model.JobTitle ?? (object)DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
            return (true, "Profile updated successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error updating profile: {ex.Message}");
        }
    }
}

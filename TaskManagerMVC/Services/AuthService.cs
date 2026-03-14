using MySql.Data.MySqlClient;
using System.Data;
using TaskManagerMVC.Data;
using TaskManagerMVC.Models;
using TaskManagerMVC.ViewModels;

namespace TaskManagerMVC.Services;

public class AuthService
{
    private readonly DbConnectionFactory _dbFactory;

    public AuthService(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_Login", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_email", email);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            Console.WriteLine($"[LOGIN DEBUG] User not found or inactive: {email}");
            return null;
        }

        var user = new User
        {
            Id = Convert.ToInt32(reader["id"]),
            FirstName = reader["first_name"].ToString()!,
            LastName = reader["last_name"].ToString()!,
            Email = reader["email"].ToString()!,
            Password = reader["password"].ToString()!,
            Phone = reader["phone"] == DBNull.Value ? null : reader["phone"].ToString(),
            IsActive = Convert.ToBoolean(reader["is_active"]),
            IsVerified = Convert.ToBoolean(reader["is_verified"]),
            CreatedAt = Convert.ToDateTime(reader["created_at"]),
            LastLogin = reader["last_login"] == DBNull.Value ? null : Convert.ToDateTime(reader["last_login"]),
            RoleId = reader["role_id"] == DBNull.Value ? 3 : Convert.ToInt32(reader["role_id"]),
            JobTitle = reader["job_title"] == DBNull.Value ? null : reader["job_title"].ToString(),
            EmployeeId = reader["employee_id"] == DBNull.Value ? null : reader["employee_id"].ToString(),
            Role = new Models.Role { Name = reader["role_name"] == DBNull.Value ? "Employee" : reader["role_name"].ToString()! }
        };
        await reader.CloseAsync();

        Console.WriteLine($"[LOGIN DEBUG] User found: {user.Email}, ID: {user.Id}");
        Console.WriteLine($"[LOGIN DEBUG] Password hash from DB: {user.Password.Substring(0, 20)}...");
        Console.WriteLine($"[LOGIN DEBUG] Attempting BCrypt verification...");
        
        bool passwordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
        Console.WriteLine($"[LOGIN DEBUG] BCrypt verification result: {passwordValid}");
        
        if (!passwordValid)
            return null;

        // Update last login
        using var updateCmd = new MySqlCommand("sp_UpdateLastLogin", conn);
        updateCmd.CommandType = CommandType.StoredProcedure;
        updateCmd.Parameters.AddWithValue("p_user_id", user.Id);
        updateCmd.Parameters.AddWithValue("p_last_login", DateTime.Now);
        await updateCmd.ExecuteNonQueryAsync();

        user.LastLogin = DateTime.Now;

        // Fetch profile image
        using var imgCmd = new MySqlCommand("sp_GetProfileImage", conn);
        imgCmd.CommandType = CommandType.StoredProcedure;
        imgCmd.Parameters.AddWithValue("p_user_id", user.Id);
        using var imgReader = await imgCmd.ExecuteReaderAsync();
        if (await imgReader.ReadAsync())
            user.ProfileImage = imgReader["profile_image"] == DBNull.Value ? null : imgReader["profile_image"].ToString();

        return user;
    }

    public async Task<(bool success, string message)> RegisterAsync(RegisterVM model)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        // Check if email exists in users table
        using var checkUserCmd = new MySqlCommand("sp_CheckEmailExists", conn);
        checkUserCmd.CommandType = CommandType.StoredProcedure;
        checkUserCmd.Parameters.AddWithValue("p_email", model.Email);
        using var userReader = await checkUserCmd.ExecuteReaderAsync();
        await userReader.ReadAsync();
        var userCount = Convert.ToInt32(userReader["count"]);
        await userReader.CloseAsync();

        if (userCount > 0)
            return (false, "Email already exists");

        // Check if email exists in pending_users table
        using var checkPendingCmd = new MySqlCommand("sp_CheckPendingEmailExists", conn);
        checkPendingCmd.CommandType = CommandType.StoredProcedure;
        checkPendingCmd.Parameters.AddWithValue("p_email", model.Email);
        using var pendingReader = await checkPendingCmd.ExecuteReaderAsync();
        await pendingReader.ReadAsync();
        var pendingCount = Convert.ToInt32(pendingReader["count"]);
        await pendingReader.CloseAsync();

        if (pendingCount > 0)
            return (false, "A registration request with this email is already pending approval");

        // Register pending user
        using var insertCmd = new MySqlCommand("sp_RegisterPendingUser", conn);
        insertCmd.CommandType = CommandType.StoredProcedure;
        insertCmd.Parameters.AddWithValue("p_first_name", model.FirstName);
        insertCmd.Parameters.AddWithValue("p_last_name", model.LastName);
        insertCmd.Parameters.AddWithValue("p_email", model.Email);
        insertCmd.Parameters.AddWithValue("p_password", BCrypt.Net.BCrypt.HashPassword(model.Password));
        insertCmd.Parameters.AddWithValue("p_phone", (object?)model.Phone ?? DBNull.Value);
        insertCmd.Parameters.AddWithValue("p_department_id", (object?)model.DepartmentId ?? DBNull.Value);
        insertCmd.Parameters.AddWithValue("p_job_title", (object?)model.JobTitle ?? DBNull.Value);
        insertCmd.Parameters.AddWithValue("p_requested_at", DateTime.Now);

        await insertCmd.ExecuteNonQueryAsync();
        return (true, "Registration request submitted successfully! Please wait for admin approval.");
    }

    public async Task<string?> CreatePasswordResetTokenAsync(string email)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        // Get user by email
        using var userCmd = new MySqlCommand("sp_CreatePasswordResetToken", conn);
        userCmd.CommandType = CommandType.StoredProcedure;
        userCmd.Parameters.AddWithValue("p_email", email);
        var userId = await userCmd.ExecuteScalarAsync();

        if (userId == null)
            return null;

        var token = Guid.NewGuid().ToString("N");

        using var insertCmd = new MySqlCommand("sp_InsertPasswordResetToken", conn);
        insertCmd.CommandType = CommandType.StoredProcedure;
        insertCmd.Parameters.AddWithValue("p_user_id", userId);
        insertCmd.Parameters.AddWithValue("p_token", token);
        insertCmd.Parameters.AddWithValue("p_expiry", DateTime.Now.AddHours(1));

        await insertCmd.ExecuteNonQueryAsync();
        return token;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        // Get reset token with user - using stored procedure
        using var cmd = new MySqlCommand("sp_ValidatePasswordResetToken", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_token", token);
        cmd.Parameters.AddWithValue("p_now", DateTime.Now);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return false;

        var resetId = Convert.ToInt32(reader["id"]);
        var userId = Convert.ToInt32(reader["user_id"]);
        await reader.CloseAsync();

        // Update user password
        using var updateUserCmd = new MySqlCommand("sp_UpdateUserPassword", conn);
        updateUserCmd.CommandType = CommandType.StoredProcedure;
        updateUserCmd.Parameters.AddWithValue("p_user_id", userId);
        updateUserCmd.Parameters.AddWithValue("p_password", BCrypt.Net.BCrypt.HashPassword(newPassword));
        await updateUserCmd.ExecuteNonQueryAsync();

        // Mark token as used
        using var updateResetCmd = new MySqlCommand("sp_MarkPasswordResetTokenUsed", conn);
        updateResetCmd.CommandType = CommandType.StoredProcedure;
        updateResetCmd.Parameters.AddWithValue("p_reset_id", resetId);
        await updateResetCmd.ExecuteNonQueryAsync();

        return true;
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetUserById", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return MapUser(reader);
    }

    private static User MapUser(IDataReader reader)
    {
        return new User
        {
            Id = Convert.ToInt32(reader["id"]),
            FirstName = reader["first_name"].ToString()!,
            LastName = reader["last_name"].ToString()!,
            Email = reader["email"].ToString()!,
            Password = reader["password"].ToString()!,
            Phone = reader["phone"] == DBNull.Value ? null : reader["phone"].ToString(),
            IsActive = Convert.ToBoolean(reader["is_active"]),
            IsVerified = Convert.ToBoolean(reader["is_verified"]),
            CreatedAt = Convert.ToDateTime(reader["created_at"]),
            LastLogin = reader["last_login"] == DBNull.Value ? null : Convert.ToDateTime(reader["last_login"]),
            RoleId = reader.GetSchemaTable()?.Columns.Contains("role_id") == true && reader["role_id"] != DBNull.Value 
                ? Convert.ToInt32(reader["role_id"]) : 3
        };
    }

    public async Task<List<(int Id, string Name)>> GetDepartmentsAsync()
    {
        var departments = new List<(int Id, string Name)>();
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetDepartments", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            departments.Add((Convert.ToInt32(reader["id"]), reader["name"].ToString()!));
        }
        return departments;
    }
}
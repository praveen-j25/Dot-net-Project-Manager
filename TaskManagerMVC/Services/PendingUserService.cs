using MySql.Data.MySqlClient;
using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskManagerMVC.Data;
using TaskManagerMVC.Models;
using TaskManagerMVC.ViewModels;

namespace TaskManagerMVC.Services;

public class PendingUserService
{
    private readonly DbConnectionFactory _dbFactory;

    public PendingUserService(DbConnectionFactory dbFactory) => _dbFactory = dbFactory;

    /// <summary>
    /// Get all pending registration requests
    /// </summary>
    public async Task<PendingUserListVM> GetPendingUsersAsync(string? statusFilter = null)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetPendingUsers", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_status_filter", (object?)statusFilter ?? DBNull.Value);

        var pendingUsers = new List<PendingUserItemVM>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            pendingUsers.Add(new PendingUserItemVM
            {
                Id = Convert.ToInt32(reader["id"]),
                FullName = $"{reader["first_name"]} {reader["last_name"]}",
                Email = reader["email"].ToString()!,
                Phone = reader["phone"] == DBNull.Value ? null : reader["phone"].ToString(),
                JobTitle = reader["job_title"] == DBNull.Value ? null : reader["job_title"].ToString(),
                DepartmentName = reader["department_name"] == DBNull.Value ? null : reader["department_name"].ToString(),
                Status = reader["status"].ToString()!,
                RequestedAt = Convert.ToDateTime(reader["requested_at"]),
                ReviewedAt = reader["reviewed_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["reviewed_at"]),
                ReviewerName = reader["reviewer_name"] == DBNull.Value ? null : reader["reviewer_name"].ToString(),
                RejectionReason = reader["rejection_reason"] == DBNull.Value ? null : reader["rejection_reason"].ToString()
            });
        }

        return new PendingUserListVM
        {
            PendingUsers = pendingUsers,
            StatusFilter = statusFilter
        };
    }

    /// <summary>
    /// Get pending user details for approval
    /// </summary>
    public async Task<ApproveUserVM?> GetPendingUserForApprovalAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetPendingUserById", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var vm = new ApproveUserVM
        {
            Id = Convert.ToInt32(reader["id"]),
            FirstName = reader["first_name"].ToString()!,
            LastName = reader["last_name"].ToString()!,
            Email = reader["email"].ToString()!,
            Phone = reader["phone"] == DBNull.Value ? null : reader["phone"].ToString(),
            JobTitle = reader["job_title"] == DBNull.Value ? null : reader["job_title"].ToString(),
            DepartmentId = reader["department_id"] == DBNull.Value ? null : Convert.ToInt32(reader["department_id"]),
            AssignedDepartmentId = reader["department_id"] == DBNull.Value ? null : Convert.ToInt32(reader["department_id"])
        };
        await reader.CloseAsync();

        // Load dropdowns
        vm.Roles = new SelectList(await GetRolesAsync(), "Id", "Name");
        vm.Departments = new SelectList(await GetDepartmentsAsync(), "Id", "Name");
        vm.Teams = new SelectList(await GetTeamsAsync(), "Id", "Name");
        vm.Managers = new SelectList(await GetManagersAsync(), "Id", "FullName");

        return vm;
    }

    /// <summary>
    /// Approve registration and create user account
    /// </summary>
    public async Task ApproveUserAsync(int pendingUserId, ApproveUserVM model, int reviewerId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // Get pending user data including password
            using var getPendingCmd = new MySqlCommand("sp_GetPendingUserPassword", conn, transaction);
            getPendingCmd.CommandType = CommandType.StoredProcedure;
            getPendingCmd.Parameters.AddWithValue("p_id", pendingUserId);

            string? hashedPassword = null;
            using (var reader = await getPendingCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    hashedPassword = reader["password"].ToString();
                }
            }

            if (string.IsNullOrEmpty(hashedPassword))
                throw new Exception("Pending user not found");

            // Auto-generate Employee ID if not provided
            if (string.IsNullOrEmpty(model.EmployeeId))
            {
                using var genCmd = new MySqlCommand("sp_GetNextEmployeeId", conn, transaction);
                genCmd.CommandType = CommandType.StoredProcedure;
                using var genReader = await genCmd.ExecuteReaderAsync();
                if (await genReader.ReadAsync())
                {
                    var lastId = genReader["employee_id"] == DBNull.Value ? null : genReader["employee_id"].ToString();
                    int nextNumber = 1;
                    if (lastId != null && lastId.StartsWith("EMP"))
                    {
                        var numberPart = lastId.Substring(3);
                        if (int.TryParse(numberPart, out int currentNumber))
                        {
                            nextNumber = currentNumber + 1;
                        }
                    }
                    model.EmployeeId = $"EMP{nextNumber:D3}";
                }
            }

            // Create user account
            using var createUserCmd = new MySqlCommand("sp_ApprovePendingUser", conn, transaction);
            createUserCmd.CommandType = CommandType.StoredProcedure;
            createUserCmd.Parameters.AddWithValue("p_first_name", model.FirstName);
            createUserCmd.Parameters.AddWithValue("p_last_name", model.LastName);
            createUserCmd.Parameters.AddWithValue("p_email", model.Email);
            createUserCmd.Parameters.AddWithValue("p_password", hashedPassword);
            createUserCmd.Parameters.AddWithValue("p_phone", (object?)model.Phone ?? DBNull.Value);
            createUserCmd.Parameters.AddWithValue("p_employee_id", model.EmployeeId);
            createUserCmd.Parameters.AddWithValue("p_job_title", (object?)model.JobTitle ?? DBNull.Value);
            createUserCmd.Parameters.AddWithValue("p_role_id", model.RoleId);
            createUserCmd.Parameters.AddWithValue("p_department_id", (object?)model.AssignedDepartmentId ?? DBNull.Value);
            createUserCmd.Parameters.AddWithValue("p_team_id", (object?)model.TeamId ?? DBNull.Value);
            createUserCmd.Parameters.AddWithValue("p_reports_to", (object?)model.ReportsTo ?? DBNull.Value);
            createUserCmd.Parameters.AddWithValue("p_hire_date", (object?)model.HireDate ?? DBNull.Value);
            createUserCmd.Parameters.AddWithValue("p_created_at", DateTime.Now);
            createUserCmd.Parameters.AddWithValue("p_pending_user_id", pendingUserId);
            createUserCmd.Parameters.AddWithValue("p_reviewed_by", reviewerId);
            createUserCmd.Parameters.AddWithValue("p_reviewed_at", DateTime.Now);

            await createUserCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Reject registration request
    /// </summary>
    public async Task RejectUserAsync(int pendingUserId, string rejectionReason, int reviewerId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_RejectPendingUser", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_id", pendingUserId);
        cmd.Parameters.AddWithValue("p_reviewed_at", DateTime.Now);
        cmd.Parameters.AddWithValue("p_reviewed_by", reviewerId);
        cmd.Parameters.AddWithValue("p_rejection_reason", rejectionReason);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Get count of pending requests
    /// </summary>
    public async Task<int> GetPendingCountAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("sp_GetPendingUserCount", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Convert.ToInt32(reader["count"]);
        }
        return 0;
    }

    // Helper methods
    private async Task<List<Role>> GetRolesAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("sp_GetRoles", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        
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

    private async Task<List<User>> GetManagersAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("sp_GetManagersAndAdmins", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        
        var managers = new List<User>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            managers.Add(new User { Id = Convert.ToInt32(reader["id"]), FirstName = reader["full_name"].ToString()!, LastName = "" });
        return managers;
    }
}

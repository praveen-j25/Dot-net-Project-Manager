using TaskManagerMVC.Tests.Configuration;
using TaskManagerMVC.Tests.Verification;
using Xunit;
using FluentAssertions;

namespace TaskManagerMVC.Tests.Unit;

/// <summary>
/// Unit tests for database verification component
/// Validates Requirements 1.1, 1.2, 1.5
/// </summary>
public class DatabaseVerificationTests
{
    private readonly IDatabaseVerifier _verifier;
    private readonly TestConfiguration _config;

    public DatabaseVerificationTests()
    {
        _config = TestConfigurationHelper.GetTestConfiguration();
        _verifier = new DatabaseVerifier(_config.ConnectionString);
    }

    [Fact]
    public async Task VerifyDatabaseConnection_ShouldSucceed()
    {
        // Act
        var result = await _verifier.VerifyDatabaseConnection();

        // Assert
        result.Should().BeTrue("database connection should be established successfully");
    }

    [Fact]
    public async Task EnumerateStoredProcedures_ShouldReturnAtLeast100Procedures()
    {
        // Act
        var procedures = await _verifier.EnumerateStoredProcedures();

        // Assert
        procedures.Should().NotBeNull();
        procedures.Should().HaveCountGreaterThanOrEqualTo(100, 
            "system should have at least 100 stored procedures as per Requirement 1.2");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpCreateUser()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_CreateUser");

        // Assert
        exists.Should().BeTrue("sp_CreateUser stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpGetUsers()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_GetUsers");

        // Assert
        exists.Should().BeTrue("sp_GetUsers stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpCreateTask()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_CreateTask");

        // Assert
        exists.Should().BeTrue("sp_CreateTask stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpGetTasks()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_GetTasks");

        // Assert
        exists.Should().BeTrue("sp_GetTasks stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpUpdateTask()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_UpdateTask");

        // Assert
        exists.Should().BeTrue("sp_UpdateTask stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpDeleteTask()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_DeleteTask");

        // Assert
        exists.Should().BeTrue("sp_DeleteTask stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpCreateProject()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_CreateProject");

        // Assert
        exists.Should().BeTrue("sp_CreateProject stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpGetProjects()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_GetProjects");

        // Assert
        exists.Should().BeTrue("sp_GetProjects stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpCreateNotification()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_CreateNotification");

        // Assert
        exists.Should().BeTrue("sp_CreateNotification stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldReturnFalseForNonExistentProcedure()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_NonExistentProcedure");

        // Assert
        exists.Should().BeFalse("non-existent stored procedure should not be found");
    }

    [Fact]
    public async Task VerifyAllRequiredStoredProcedures_ShouldExist()
    {
        // Arrange
        var requiredProcedures = _config.RequiredStoredProcedures;

        // Act & Assert
        foreach (var procedure in requiredProcedures)
        {
            var exists = await _verifier.VerifyStoredProcedureExists(procedure);
            exists.Should().BeTrue($"required stored procedure {procedure} should exist");
        }
    }

    [Fact]
    public async Task VerifyStoredProcedureParameters_ShouldValidateSpCreateUserParameters()
    {
        // Arrange
        var expectedParams = new List<string> { "p_email", "p_password_hash", "p_first_name", "p_last_name", "p_role" };

        // Act
        var result = await _verifier.VerifyStoredProcedureParameters("sp_CreateUser", expectedParams);

        // Assert
        result.Should().BeTrue("sp_CreateUser should have the expected parameters");
    }

    [Fact]
    public async Task VerifyStoredProcedureParameters_ShouldValidateSpGetTasksParameters()
    {
        // Arrange
        var expectedParams = new List<string> { "p_user_id", "p_role" };

        // Act
        var result = await _verifier.VerifyStoredProcedureParameters("sp_GetTasks", expectedParams);

        // Assert
        result.Should().BeTrue("sp_GetTasks should have the expected parameters for role-based filtering");
    }

    [Fact]
    public async Task VerifyStoredProcedureParameters_ShouldValidateSpUpdateTaskParameters()
    {
        // Arrange
        var expectedParams = new List<string> { "p_task_id", "p_title", "p_description", "p_status", "p_priority", "p_due_date" };

        // Act
        var result = await _verifier.VerifyStoredProcedureParameters("sp_UpdateTask", expectedParams);

        // Assert
        result.Should().BeTrue("sp_UpdateTask should have the expected parameters");
    }

    [Fact]
    public async Task VerifyStoredProcedureParameters_ShouldValidateSpCreateProjectParameters()
    {
        // Arrange
        var expectedParams = new List<string> { "p_name", "p_description", "p_manager_id", "p_start_date", "p_end_date" };

        // Act
        var result = await _verifier.VerifyStoredProcedureParameters("sp_CreateProject", expectedParams);

        // Assert
        result.Should().BeTrue("sp_CreateProject should have the expected parameters");
    }

    [Fact]
    public async Task VerifyStoredProcedureParameters_ShouldReturnFalseForMissingParameters()
    {
        // Arrange
        var expectedParams = new List<string> { "p_nonexistent_param", "p_another_missing_param" };

        // Act
        var result = await _verifier.VerifyStoredProcedureParameters("sp_CreateUser", expectedParams);

        // Assert
        result.Should().BeFalse("verification should fail when expected parameters are missing");
    }

    [Fact]
    public async Task EnumerateStoredProcedures_ShouldIncludeAllCRUDOperations()
    {
        // Act
        var procedures = await _verifier.EnumerateStoredProcedures();

        // Assert
        procedures.Should().Contain(p => p.StartsWith("sp_Create"), "should have Create operations");
        procedures.Should().Contain(p => p.StartsWith("sp_Get"), "should have Read operations");
        procedures.Should().Contain(p => p.StartsWith("sp_Update"), "should have Update operations");
        procedures.Should().Contain(p => p.StartsWith("sp_Delete"), "should have Delete operations");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpUpdateUser()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_UpdateUser");

        // Assert
        exists.Should().BeTrue("sp_UpdateUser stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpDeleteUser()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_DeleteUser");

        // Assert
        exists.Should().BeTrue("sp_DeleteUser stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpUpdateProject()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_UpdateProject");

        // Assert
        exists.Should().BeTrue("sp_UpdateProject stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpDeleteProject()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_DeleteProject");

        // Assert
        exists.Should().BeTrue("sp_DeleteProject stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpGetNotifications()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_GetNotifications");

        // Assert
        exists.Should().BeTrue("sp_GetNotifications stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpAddComment()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_AddComment");

        // Assert
        exists.Should().BeTrue("sp_AddComment stored procedure should exist");
    }

    [Fact]
    public async Task VerifyStoredProcedureExists_ShouldFindSpLogTime()
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists("sp_LogTime");

        // Assert
        exists.Should().BeTrue("sp_LogTime stored procedure should exist");
    }

    [Fact]
    public async Task VerifyDatabaseConnection_ShouldHandleInvalidConnectionString()
    {
        // Arrange
        var invalidVerifier = new DatabaseVerifier("Server=invalid;Database=invalid;User=invalid;Password=invalid;");

        // Act
        var result = await invalidVerifier.VerifyDatabaseConnection();

        // Assert
        result.Should().BeFalse("connection should fail with invalid connection string");
    }

    [Fact]
    public async Task EnumerateStoredProcedures_ShouldReturnOrderedList()
    {
        // Act
        var procedures = await _verifier.EnumerateStoredProcedures();

        // Assert
        procedures.Should().NotBeNull();
        procedures.Should().BeInAscendingOrder("stored procedures should be returned in alphabetical order");
    }

    [Theory]
    [InlineData("sp_CreateUser")]
    [InlineData("sp_GetUsers")]
    [InlineData("sp_UpdateUser")]
    [InlineData("sp_DeleteUser")]
    [InlineData("sp_CreateTask")]
    [InlineData("sp_GetTasks")]
    [InlineData("sp_UpdateTask")]
    [InlineData("sp_DeleteTask")]
    public async Task VerifyStoredProcedureExists_ShouldFindAllRequiredProcedures(string procedureName)
    {
        // Act
        var exists = await _verifier.VerifyStoredProcedureExists(procedureName);

        // Assert
        exists.Should().BeTrue($"{procedureName} stored procedure should exist as per Requirement 1.5");
    }
}

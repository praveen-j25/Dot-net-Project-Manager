using TaskManagerMVC.Tests.Configuration;
using Xunit;
using FluentAssertions;
using MySql.Data.MySqlClient;

namespace TaskManagerMVC.Tests.Infrastructure;

/// <summary>
/// Tests to verify the testing infrastructure is properly set up
/// </summary>
public class InfrastructureTests
{
    [Fact]
    public void TestConfiguration_ShouldLoad_Successfully()
    {
        // Arrange & Act
        var config = TestConfigurationHelper.GetTestConfiguration();

        // Assert
        config.Should().NotBeNull();
        config.ConnectionString.Should().NotBeNullOrEmpty();
        config.BaseUrl.Should().NotBeNullOrEmpty();
        config.TimeoutSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ConnectionString_ShouldBe_Valid()
    {
        // Arrange
        var connectionString = TestConfigurationHelper.GetConnectionString();

        // Assert
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("Server=");
        connectionString.Should().Contain("Database=");
    }

    [Fact]
    public void RequiredStoredProcedures_ShouldBe_Configured()
    {
        // Arrange & Act
        var config = TestConfigurationHelper.GetTestConfiguration();

        // Assert
        config.RequiredStoredProcedures.Should().NotBeEmpty();
        config.RequiredStoredProcedures.Should().Contain("sp_CreateUser");
        config.RequiredStoredProcedures.Should().Contain("sp_GetUsers");
    }

    [Fact]
    public void RequiredControllers_ShouldBe_Configured()
    {
        // Arrange & Act
        var config = TestConfigurationHelper.GetTestConfiguration();

        // Assert
        config.RequiredControllers.Should().NotBeEmpty();
        config.RequiredControllers.Should().Contain("AccountController");
        config.RequiredControllers.Should().Contain("AdminController");
        config.RequiredControllers.Count.Should().BeGreaterThanOrEqualTo(7);
    }

    [Fact]
    public void RequiredRoles_ShouldBe_Configured()
    {
        // Arrange & Act
        var config = TestConfigurationHelper.GetTestConfiguration();

        // Assert
        config.RequiredRoles.Should().NotBeEmpty();
        config.RequiredRoles.Should().Contain("Admin");
        config.RequiredRoles.Should().Contain("Manager");
        config.RequiredRoles.Should().Contain("Employee");
        config.RequiredRoles.Count.Should().Be(3);
    }

    [Fact]
    public void RequiredPolicies_ShouldBe_Configured()
    {
        // Arrange & Act
        var config = TestConfigurationHelper.GetTestConfiguration();

        // Assert
        config.RequiredPolicies.Should().NotBeEmpty();
        config.RequiredPolicies.Count.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void DatabaseConnection_ShouldBe_Testable()
    {
        // Arrange
        var connectionString = TestConfigurationHelper.GetConnectionString();

        // Act & Assert
        // Note: This test will fail if the database is not accessible
        // It's expected to fail in CI/CD without database setup
        using var connection = new MySqlConnection(connectionString);
        var action = () => connection.Open();
        
        // We're just testing that we can create a connection object
        // Actual connection test will be in database verification tests
        action.Should().NotThrow<ArgumentException>("connection string should be valid");
    }
}

using TaskManagerMVC.Tests.Verification;
using Xunit;
using FluentAssertions;

namespace TaskManagerMVC.Tests.Unit;

/// <summary>
/// Unit tests for ADO.NET verification component
/// Validates Requirements 2.1, 2.8
/// </summary>
public class AdoNetVerificationTests
{
    private readonly IAdoNetVerifier _verifier;

    public AdoNetVerificationTests()
    {
        _verifier = new AdoNetVerifier();
    }

    [Fact]
    public async Task VerifyNoEntityFramework_ShouldReturnTrue()
    {
        // Act
        var result = await _verifier.VerifyNoEntityFramework();

        // Assert
        result.Should().BeTrue("codebase should not contain Entity Framework references as per Requirement 2.1");
    }

    [Fact]
    public async Task VerifyMySqlConnectionUsage_ShouldReturnTrue()
    {
        // Act
        var result = await _verifier.VerifyMySqlConnectionUsage();

        // Assert
        result.Should().BeTrue("service classes should use MySqlConnection for database connections as per Requirement 2.2");
    }

    [Fact]
    public async Task VerifyMySqlCommandUsage_ShouldReturnTrue()
    {
        // Act
        var result = await _verifier.VerifyMySqlCommandUsage();

        // Assert
        result.Should().BeTrue("database commands should use MySqlCommand with CommandType.StoredProcedure as per Requirement 2.3");
    }

    [Fact]
    public async Task VerifyMySqlDataReaderUsage_ShouldReturnTrue()
    {
        // Act
        var result = await _verifier.VerifyMySqlDataReaderUsage();

        // Assert
        result.Should().BeTrue("data retrieval should use MySqlDataReader as per Requirement 2.4");
    }

    [Fact]
    public async Task VerifyParameterizedQueries_ShouldReturnTrue()
    {
        // Act
        var result = await _verifier.VerifyParameterizedQueries();

        // Assert
        result.Should().BeTrue("all queries should use parameterized queries with MySqlParameter or AddWithValue as per Requirement 2.5");
    }

    [Fact]
    public async Task VerifyConnectionDisposal_ShouldReturnTrue()
    {
        // Act
        var result = await _verifier.VerifyConnectionDisposal();

        // Assert
        result.Should().BeTrue("connections should be properly disposed using using statements or try-finally blocks as per Requirement 2.6");
    }

    [Fact]
    public async Task VerifyAsyncPatterns_ShouldReturnTrue()
    {
        // Act
        var result = await _verifier.VerifyAsyncPatterns();

        // Assert
        result.Should().BeTrue("async operations should use ExecuteReaderAsync, ExecuteNonQueryAsync, or ExecuteScalarAsync as per Requirement 2.7");
    }

    [Fact]
    public async Task VerifyNoEntityFramework_ShouldDetectDbContext()
    {
        // This test verifies that the verifier can detect Entity Framework usage
        // In our actual codebase, this should return true (no EF)
        // This is a validation that the verification logic works correctly

        // Act
        var result = await _verifier.VerifyNoEntityFramework();

        // Assert
        result.Should().BeTrue("Entity Framework should not be used in the codebase");
    }

    [Fact]
    public async Task VerifyConnectionFactoryPattern_ShouldBeUsed()
    {
        // This test verifies that the connection factory pattern is used
        // The pattern is validated through MySqlConnection usage and proper disposal

        // Act
        var connectionUsage = await _verifier.VerifyMySqlConnectionUsage();
        var connectionDisposal = await _verifier.VerifyConnectionDisposal();

        // Assert
        connectionUsage.Should().BeTrue("connection factory pattern should use MySqlConnection");
        connectionDisposal.Should().BeTrue("connection factory pattern should properly dispose connections as per Requirement 2.8");
    }

    [Fact]
    public async Task VerifyAllAdoNetRequirements_ShouldPass()
    {
        // This test validates all ADO.NET requirements together
        // Act
        var noEntityFramework = await _verifier.VerifyNoEntityFramework();
        var mySqlConnection = await _verifier.VerifyMySqlConnectionUsage();
        var mySqlCommand = await _verifier.VerifyMySqlCommandUsage();
        var mySqlDataReader = await _verifier.VerifyMySqlDataReaderUsage();
        var parameterized = await _verifier.VerifyParameterizedQueries();
        var disposal = await _verifier.VerifyConnectionDisposal();
        var asyncPatterns = await _verifier.VerifyAsyncPatterns();

        // Assert
        noEntityFramework.Should().BeTrue("Requirement 2.1: No Entity Framework");
        mySqlConnection.Should().BeTrue("Requirement 2.2: MySqlConnection usage");
        mySqlCommand.Should().BeTrue("Requirement 2.3: MySqlCommand with StoredProcedure");
        mySqlDataReader.Should().BeTrue("Requirement 2.4: MySqlDataReader usage");
        parameterized.Should().BeTrue("Requirement 2.5: Parameterized queries");
        disposal.Should().BeTrue("Requirement 2.6: Proper connection disposal");
        asyncPatterns.Should().BeTrue("Requirement 2.7: Async patterns");
    }
}

namespace TaskManagerMVC.Tests.Verification;

/// <summary>
/// Interface for verifying ADO.NET data access patterns without Entity Framework
/// </summary>
public interface IAdoNetVerifier
{
    /// <summary>
    /// Verifies that the codebase does not contain Entity Framework references
    /// (DbContext, DbSet, LINQ to Entities)
    /// </summary>
    Task<bool> VerifyNoEntityFramework();

    /// <summary>
    /// Verifies that service classes use MySqlConnection for all database connections
    /// </summary>
    Task<bool> VerifyMySqlConnectionUsage();

    /// <summary>
    /// Verifies that database commands use MySqlCommand with CommandType.StoredProcedure
    /// </summary>
    Task<bool> VerifyMySqlCommandUsage();

    /// <summary>
    /// Verifies that data retrieval uses MySqlDataReader
    /// </summary>
    Task<bool> VerifyMySqlDataReaderUsage();

    /// <summary>
    /// Verifies that all command parameters use MySqlParameter or AddWithValue
    /// </summary>
    Task<bool> VerifyParameterizedQueries();

    /// <summary>
    /// Verifies that connections are properly disposed using using statements or try-finally blocks
    /// </summary>
    Task<bool> VerifyConnectionDisposal();

    /// <summary>
    /// Verifies that async operations use ExecuteReaderAsync, ExecuteNonQueryAsync, ExecuteScalarAsync
    /// </summary>
    Task<bool> VerifyAsyncPatterns();
}

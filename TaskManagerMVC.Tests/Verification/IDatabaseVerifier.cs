namespace TaskManagerMVC.Tests.Verification;

/// <summary>
/// Interface for database verification operations
/// </summary>
public interface IDatabaseVerifier
{
    /// <summary>
    /// Verifies that the database connection can be established
    /// </summary>
    Task<bool> VerifyDatabaseConnection();

    /// <summary>
    /// Enumerates all stored procedures in the database
    /// </summary>
    Task<List<string>> EnumerateStoredProcedures();

    /// <summary>
    /// Verifies that a specific stored procedure exists
    /// </summary>
    Task<bool> VerifyStoredProcedureExists(string procedureName);

    /// <summary>
    /// Verifies that a stored procedure has the expected parameters
    /// </summary>
    Task<bool> VerifyStoredProcedureParameters(string procedureName, List<string> expectedParams);

    /// <summary>
    /// Tests execution of a stored procedure with provided parameters
    /// </summary>
    Task<bool> TestStoredProcedureExecution(string procedureName, Dictionary<string, object> parameters);
}

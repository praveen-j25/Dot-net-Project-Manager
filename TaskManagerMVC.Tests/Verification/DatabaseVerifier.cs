using MySql.Data.MySqlClient;
using System.Data;

namespace TaskManagerMVC.Tests.Verification;

/// <summary>
/// Implementation of database verification operations
/// </summary>
public class DatabaseVerifier : IDatabaseVerifier
{
    private readonly string _connectionString;

    public DatabaseVerifier(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyDatabaseConnection()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection.State == ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<List<string>> EnumerateStoredProcedures()
    {
        var procedures = new List<string>();

        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT ROUTINE_NAME 
                FROM INFORMATION_SCHEMA.ROUTINES 
                WHERE ROUTINE_TYPE = 'PROCEDURE' 
                AND ROUTINE_SCHEMA = DATABASE()
                ORDER BY ROUTINE_NAME";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                procedures.Add(reader.GetString(0));
            }
        }
        catch
        {
            // Return empty list on error
        }

        return procedures;
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyStoredProcedureExists(string procedureName)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.ROUTINES 
                WHERE ROUTINE_TYPE = 'PROCEDURE' 
                AND ROUTINE_SCHEMA = DATABASE()
                AND ROUTINE_NAME = @ProcedureName";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProcedureName", procedureName);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyStoredProcedureParameters(string procedureName, List<string> expectedParams)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT PARAMETER_NAME 
                FROM INFORMATION_SCHEMA.PARAMETERS 
                WHERE SPECIFIC_SCHEMA = DATABASE()
                AND SPECIFIC_NAME = @ProcedureName
                AND PARAMETER_NAME IS NOT NULL
                ORDER BY ORDINAL_POSITION";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProcedureName", procedureName);

            var actualParams = new List<string>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var paramName = reader.GetString(0);
                // Remove the @ prefix if present
                actualParams.Add(paramName.TrimStart('@'));
            }

            // Check if all expected parameters are present
            foreach (var expectedParam in expectedParams)
            {
                var cleanExpected = expectedParam.TrimStart('@');
                if (!actualParams.Any(p => p.Equals(cleanExpected, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TestStoredProcedureExecution(string procedureName, Dictionary<string, object> parameters)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(procedureName, connection);
            command.CommandType = CommandType.StoredProcedure;

            // Add parameters
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value);
            }

            // Execute the stored procedure
            await command.ExecuteNonQueryAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }
}

using System.Text.RegularExpressions;

namespace TaskManagerMVC.Tests.Verification;

/// <summary>
/// Verifies ADO.NET data access patterns without Entity Framework
/// </summary>
public class AdoNetVerifier : IAdoNetVerifier
{
    private readonly string _servicesPath;
    private readonly List<string> _serviceFiles;

    public AdoNetVerifier(string servicesPath = "../../../../TaskManagerMVC/Services")
    {
        _servicesPath = servicesPath;
        _serviceFiles = new List<string>();
    }

    /// <summary>
    /// Loads all service files for analysis
    /// </summary>
    private void LoadServiceFiles()
    {
        if (_serviceFiles.Count > 0) return;

        var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _servicesPath));
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Services directory not found: {fullPath}");
        }

        var files = Directory.GetFiles(fullPath, "*.cs", SearchOption.TopDirectoryOnly);
        _serviceFiles.AddRange(files);
    }

    /// <summary>
    /// Reads all service file contents
    /// </summary>
    private async Task<List<string>> GetAllServiceContents()
    {
        LoadServiceFiles();
        var contents = new List<string>();

        foreach (var file in _serviceFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            contents.Add(content);
        }

        return contents;
    }

    public async Task<bool> VerifyNoEntityFramework()
    {
        var contents = await GetAllServiceContents();
        var entityFrameworkPatterns = new[]
        {
            @"\bDbContext\b",
            @"\bDbSet\b",
            @"\bDbSet<",
            @"\.AsQueryable\(\)",
            @"\.AsNoTracking\(\)",
            @"\.Include\(",
            @"\.ThenInclude\(",
            @"using\s+Microsoft\.EntityFrameworkCore",
            @"using\s+System\.Linq"
        };

        foreach (var content in contents)
        {
            foreach (var pattern in entityFrameworkPatterns)
            {
                if (Regex.IsMatch(content, pattern))
                {
                    // Allow System.Linq for basic LINQ operations on collections, not database queries
                    if (pattern.Contains("System.Linq"))
                    {
                        // Check if it's used with database context
                        if (Regex.IsMatch(content, @"DbContext|DbSet"))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public async Task<bool> VerifyMySqlConnectionUsage()
    {
        var contents = await GetAllServiceContents();
        var hasMySqlConnection = false;
        var hasOtherConnection = false;

        foreach (var content in contents)
        {
            // Check for MySqlConnection usage
            if (Regex.IsMatch(content, @"\bMySqlConnection\b"))
            {
                hasMySqlConnection = true;
            }

            // Check for other database connection types (SqlConnection, NpgsqlConnection, etc.)
            if (Regex.IsMatch(content, @"\b(SqlConnection|NpgsqlConnection|OracleConnection|SqliteConnection)\b"))
            {
                hasOtherConnection = true;
            }
        }

        // Should have MySqlConnection and no other connection types
        return hasMySqlConnection && !hasOtherConnection;
    }

    public async Task<bool> VerifyMySqlCommandUsage()
    {
        var contents = await GetAllServiceContents();
        var hasMySqlCommand = false;
        var hasStoredProcedureType = false;

        foreach (var content in contents)
        {
            // Check for MySqlCommand usage
            if (Regex.IsMatch(content, @"\bMySqlCommand\b"))
            {
                hasMySqlCommand = true;
            }

            // Check for CommandType.StoredProcedure
            if (Regex.IsMatch(content, @"CommandType\.StoredProcedure"))
            {
                hasStoredProcedureType = true;
            }

            // Check for inline SQL (should not exist)
            if (Regex.IsMatch(content, @"CommandType\.Text"))
            {
                return false;
            }
        }

        return hasMySqlCommand && hasStoredProcedureType;
    }

    public async Task<bool> VerifyMySqlDataReaderUsage()
    {
        var contents = await GetAllServiceContents();
        var hasMySqlDataReader = false;

        foreach (var content in contents)
        {
            // Check for MySqlDataReader usage
            if (Regex.IsMatch(content, @"\bMySqlDataReader\b"))
            {
                hasMySqlDataReader = true;
            }

            // Also check for ExecuteReader which returns MySqlDataReader
            if (Regex.IsMatch(content, @"ExecuteReader(Async)?\("))
            {
                hasMySqlDataReader = true;
            }
        }

        return hasMySqlDataReader;
    }

    public async Task<bool> VerifyParameterizedQueries()
    {
        var contents = await GetAllServiceContents();
        var hasParameterization = false;

        foreach (var content in contents)
        {
            // Check for MySqlParameter usage
            if (Regex.IsMatch(content, @"\bMySqlParameter\b"))
            {
                hasParameterization = true;
            }

            // Check for AddWithValue usage
            if (Regex.IsMatch(content, @"\.AddWithValue\("))
            {
                hasParameterization = true;
            }

            // Check for Parameters.Add usage
            if (Regex.IsMatch(content, @"\.Parameters\.Add\("))
            {
                hasParameterization = true;
            }

            // Check for string concatenation in SQL (bad practice)
            if (Regex.IsMatch(content, @"(SELECT|INSERT|UPDATE|DELETE).*\+.*@"))
            {
                return false;
            }
        }

        return hasParameterization;
    }

    public async Task<bool> VerifyConnectionDisposal()
    {
        var contents = await GetAllServiceContents();
        var hasUsingStatements = false;

        foreach (var content in contents)
        {
            // Check for using statements with MySqlConnection (traditional syntax)
            if (Regex.IsMatch(content, @"using\s*\([^)]*MySqlConnection"))
            {
                hasUsingStatements = true;
            }

            // Check for using var/using declarations (modern C# syntax)
            // Pattern: using var conn = _dbFactory.CreateConnection();
            if (Regex.IsMatch(content, @"using\s+var\s+\w+\s*=\s*\w+\.CreateConnection\(\)"))
            {
                hasUsingStatements = true;
            }

            // Check for using var with new MySqlConnection
            if (Regex.IsMatch(content, @"using\s+var\s+\w+\s*=\s*new\s+MySqlConnection"))
            {
                hasUsingStatements = true;
            }

            // Check for await using statements
            if (Regex.IsMatch(content, @"await\s+using\s*\([^)]*MySqlConnection"))
            {
                hasUsingStatements = true;
            }

            // Check for await using var declarations
            if (Regex.IsMatch(content, @"await\s+using\s+var\s+\w+\s*=.*MySqlConnection"))
            {
                hasUsingStatements = true;
            }

            // Check for try-finally with Dispose
            if (Regex.IsMatch(content, @"try\s*\{[\s\S]*?\}\s*finally\s*\{[\s\S]*?\.Dispose\(\)"))
            {
                hasUsingStatements = true;
            }
        }

        return hasUsingStatements;
    }

    public async Task<bool> VerifyAsyncPatterns()
    {
        var contents = await GetAllServiceContents();
        var hasAsyncMethods = false;

        foreach (var content in contents)
        {
            // Check for async database operations
            if (Regex.IsMatch(content, @"ExecuteReaderAsync\("))
            {
                hasAsyncMethods = true;
            }

            if (Regex.IsMatch(content, @"ExecuteNonQueryAsync\("))
            {
                hasAsyncMethods = true;
            }

            if (Regex.IsMatch(content, @"ExecuteScalarAsync\("))
            {
                hasAsyncMethods = true;
            }

            // Check for synchronous methods (should be avoided)
            // Allow some synchronous calls but prefer async
            var syncCount = Regex.Matches(content, @"\.ExecuteReader\(\)").Count;
            var asyncCount = Regex.Matches(content, @"\.ExecuteReaderAsync\(").Count;

            // If there are synchronous calls but no async calls, that's a problem
            if (syncCount > 0 && asyncCount == 0)
            {
                // This file doesn't use async patterns
                continue;
            }
        }

        return hasAsyncMethods;
    }
}

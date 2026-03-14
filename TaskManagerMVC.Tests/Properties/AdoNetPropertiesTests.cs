using FsCheck;
using FsCheck.Xunit;
using FluentAssertions;
using System.Text.RegularExpressions;

namespace TaskManagerMVC.Tests.Properties;

/// <summary>
/// Property-based tests for ADO.NET usage patterns
/// Validates universal properties across all service methods
/// **Validates: Requirements 2.2, 2.3, 2.4, 2.6**
/// </summary>
public class AdoNetPropertiesTests
{
    private readonly string _servicesPath;
    private readonly List<string> _serviceFiles;

    public AdoNetPropertiesTests()
    {
        _servicesPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../TaskManagerMVC/Services"));
        _serviceFiles = new List<string>();
        LoadServiceFiles();
    }

    /// <summary>
    /// Loads all service files for analysis
    /// </summary>
    private void LoadServiceFiles()
    {
        if (!Directory.Exists(_servicesPath))
        {
            throw new DirectoryNotFoundException($"Services directory not found: {_servicesPath}");
        }

        var files = Directory.GetFiles(_servicesPath, "*.cs", SearchOption.TopDirectoryOnly);
        _serviceFiles.AddRange(files);
    }

    /// <summary>
    /// Property 5: Service Classes Use MySqlConnection
    /// **Validates: Requirements 2.2**
    /// 
    /// For any service class that accesses the database, 
    /// the class SHALL use MySqlConnection for all database connections.
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "end-to-end-testing-verification")]
    [Trait("Property", "5: Service Classes Use MySqlConnection")]
    public bool ServiceClassesUseMySqlConnection(int fileIndex)
    {
        var actualIndex = Math.Abs(fileIndex) % _serviceFiles.Count;
        var serviceFile = _serviceFiles[actualIndex];
        
        var content = File.ReadAllText(serviceFile);
        var fileName = Path.GetFileName(serviceFile);

        // Check if this file has any database operations
        var hasDbOperations = Regex.IsMatch(content, @"(CreateConnection|MySqlCommand|MySqlDataReader)");
        
        if (!hasDbOperations)
        {
            // No database operations - property holds trivially
            return true;
        }

        // Check for MySqlConnection usage
        var hasMySqlConnection = Regex.IsMatch(content, @"\bMySqlConnection\b") || 
                                 Regex.IsMatch(content, @"CreateConnection\(\)");

        if (!hasMySqlConnection)
        {
            throw new Exception($"✗ {fileName}: Service has database operations but does not use MySqlConnection");
        }

        // Check for other database connection types (should not exist)
        var otherConnectionPatterns = new[]
        {
            @"\bSqlConnection\b",
            @"\bNpgsqlConnection\b",
            @"\bOracleConnection\b",
            @"\bSqliteConnection\b"
        };

        foreach (var pattern in otherConnectionPatterns)
        {
            if (Regex.IsMatch(content, pattern))
            {
                var match = Regex.Match(content, pattern);
                throw new Exception($"✗ {fileName}: Found non-MySQL connection type at position {match.Index}: {match.Value}");
            }
        }

        return true;
    }

    /// <summary>
    /// Property 6: Database Commands Use MySqlCommand with StoredProcedure
    /// **Validates: Requirements 2.3**
    /// 
    /// For any database command execution, 
    /// the code SHALL use MySqlCommand with CommandType.StoredProcedure.
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "end-to-end-testing-verification")]
    [Trait("Property", "6: Database Commands Use MySqlCommand with StoredProcedure")]
    public bool DatabaseCommandsUseMySqlCommandWithStoredProcedure(int fileIndex)
    {
        var actualIndex = Math.Abs(fileIndex) % _serviceFiles.Count;
        var serviceFile = _serviceFiles[actualIndex];
        
        var content = File.ReadAllText(serviceFile);
        var fileName = Path.GetFileName(serviceFile);

        // Find all MySqlCommand instantiations
        var commandPattern = @"new\s+MySqlCommand\s*\([^)]+\)";
        var commandMatches = Regex.Matches(content, commandPattern);

        if (commandMatches.Count == 0)
        {
            // No MySqlCommand usage - property holds trivially
            return true;
        }

        // For each MySqlCommand, verify CommandType.StoredProcedure is set
        foreach (Match match in commandMatches)
        {
            var commandPosition = match.Index;
            
            // Extract context around this command (next 300 characters)
            var contextLength = Math.Min(300, content.Length - commandPosition);
            var context = content.Substring(commandPosition, contextLength);

            // Check if CommandType.StoredProcedure is set
            var hasStoredProcedureType = Regex.IsMatch(context, @"CommandType\s*=\s*CommandType\.StoredProcedure");

            if (!hasStoredProcedureType)
            {
                // Check if it's using CommandType.Text (violation)
                if (Regex.IsMatch(context, @"CommandType\s*=\s*CommandType\.Text"))
                {
                    throw new Exception($"✗ {fileName}: MySqlCommand uses CommandType.Text instead of StoredProcedure at position {commandPosition}");
                }

                // If no CommandType is set, check if it's a stored procedure call
                var spMatch = Regex.Match(context, @"new\s+MySqlCommand\s*\(\s*""(sp_[^""]+)""");
                if (spMatch.Success)
                {
                    throw new Exception($"✗ {fileName}: Stored procedure '{spMatch.Groups[1].Value}' called without CommandType.StoredProcedure at position {commandPosition}");
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Property 7: Data Retrieval Uses MySqlDataReader
    /// **Validates: Requirements 2.4**
    /// 
    /// For any data retrieval operation that returns multiple rows, 
    /// the code SHALL use MySqlDataReader to read query results.
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "end-to-end-testing-verification")]
    [Trait("Property", "7: Data Retrieval Uses MySqlDataReader")]
    public bool DataRetrievalUsesMySqlDataReader(int fileIndex)
    {
        var actualIndex = Math.Abs(fileIndex) % _serviceFiles.Count;
        var serviceFile = _serviceFiles[actualIndex];
        
        var content = File.ReadAllText(serviceFile);
        var fileName = Path.GetFileName(serviceFile);

        // Check if this file has ExecuteReader calls
        var executeReaderPattern = @"(ExecuteReader|ExecuteReaderAsync)\s*\(";
        var executeReaderMatches = Regex.Matches(content, executeReaderPattern);

        if (executeReaderMatches.Count == 0)
        {
            // No ExecuteReader calls - property holds trivially
            return true;
        }

        // For each ExecuteReader call, verify the result is assigned to MySqlDataReader
        foreach (Match match in executeReaderMatches)
        {
            var position = match.Index;
            
            // Look backwards to find the variable assignment
            var beforeContext = content.Substring(Math.Max(0, position - 200), Math.Min(200, position));
            
            // Check for MySqlDataReader variable declaration
            var readerVarPattern = @"(var|MySqlDataReader)\s+(\w+)\s*=\s*await\s*$";
            var readerMatch = Regex.Match(beforeContext, readerVarPattern);

            if (!readerMatch.Success)
            {
                // Try without await
                readerVarPattern = @"(var|MySqlDataReader)\s+(\w+)\s*=\s*$";
                readerMatch = Regex.Match(beforeContext, readerVarPattern);
            }

            // Also check for using var reader pattern
            var usingReaderPattern = @"using\s+var\s+(\w+)\s*=\s*await\s*$";
            var usingMatch = Regex.Match(beforeContext, usingReaderPattern);

            if (!readerMatch.Success && !usingMatch.Success)
            {
                // ExecuteReader result might not be properly assigned to a reader variable
                // This is acceptable if it's used inline, but let's verify it's not ignored
                var afterContext = content.Substring(position, Math.Min(100, content.Length - position));
                
                // Check if the result is used (not just discarded)
                if (!Regex.IsMatch(afterContext, @"\)\s*;"))
                {
                    // Result is being used, which is good
                    continue;
                }
            }
        }

        // Verify MySqlDataReader is actually used in the file
        var hasMySqlDataReader = Regex.IsMatch(content, @"\bMySqlDataReader\b") || 
                                 Regex.IsMatch(content, @"using\s+var\s+\w+\s*=\s*await\s+\w+\.ExecuteReaderAsync");

        if (executeReaderMatches.Count > 0 && !hasMySqlDataReader)
        {
            throw new Exception($"✗ {fileName}: ExecuteReader is called but MySqlDataReader is not explicitly used");
        }

        return true;
    }

    /// <summary>
    /// Property 9: Connections Are Properly Disposed
    /// **Validates: Requirements 2.6**
    /// 
    /// For any database connection usage, 
    /// the connection SHALL be disposed using a using statement or try-finally block.
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "end-to-end-testing-verification")]
    [Trait("Property", "9: Connections Are Properly Disposed")]
    public bool ConnectionsAreProperlyDisposed(int fileIndex)
    {
        var actualIndex = Math.Abs(fileIndex) % _serviceFiles.Count;
        var serviceFile = _serviceFiles[actualIndex];
        
        var content = File.ReadAllText(serviceFile);
        var fileName = Path.GetFileName(serviceFile);

        // Check if this file creates connections
        var connectionPattern = @"(CreateConnection\(\)|new\s+MySqlConnection)";
        var connectionMatches = Regex.Matches(content, connectionPattern);

        if (connectionMatches.Count == 0)
        {
            // No connections created - property holds trivially
            return true;
        }

        // For each connection creation, verify it's in a using statement
        foreach (Match match in connectionMatches)
        {
            var position = match.Index;
            
            // Look backwards to find the using statement (up to 150 characters)
            var beforeContext = content.Substring(Math.Max(0, position - 150), Math.Min(150, position));
            
            // Check for using var pattern (modern C#) - look for the pattern on the same line
            var hasUsingVar = Regex.IsMatch(beforeContext, @"using\s+var\s+\w+\s*=\s*[^;]*$");
            
            // Check for using statement pattern (traditional)
            var hasUsing = Regex.IsMatch(beforeContext, @"using\s*\(\s*var\s+\w+\s*=\s*[^;]*$");
            
            // Check for await using pattern
            var hasAwaitUsing = Regex.IsMatch(beforeContext, @"await\s+using\s+(var\s+)?\w+\s*=\s*[^;]*$");

            if (!hasUsingVar && !hasUsing && !hasAwaitUsing)
            {
                // Check if it's in a try-finally block with Dispose
                var methodContext = GetMethodContext(content, position);
                var hasTryFinally = Regex.IsMatch(methodContext, @"try\s*\{[\s\S]*?\}\s*finally\s*\{[\s\S]*?\.Dispose\(\)");

                if (!hasTryFinally)
                {
                    throw new Exception($"✗ {fileName}: Connection created at position {position} is not properly disposed with using statement or try-finally");
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Helper method to extract the method context around a position
    /// </summary>
    private string GetMethodContext(string content, int position)
    {
        // Find the method start (look backwards for method signature)
        var methodStart = position;
        for (int i = position; i >= 0; i--)
        {
            if (content[i] == '{' && i > 0)
            {
                // Check if this is a method opening brace
                var beforeBrace = content.Substring(Math.Max(0, i - 200), Math.Min(200, i));
                if (Regex.IsMatch(beforeBrace, @"(public|private|protected|internal)\s+(async\s+)?(Task|void|bool|int|string)"))
                {
                    methodStart = i;
                    break;
                }
            }
        }

        // Find the method end (matching closing brace)
        var methodEnd = position;
        var braceCount = 0;
        for (int i = methodStart; i < content.Length; i++)
        {
            if (content[i] == '{') braceCount++;
            if (content[i] == '}') braceCount--;
            if (braceCount == 0 && i > methodStart)
            {
                methodEnd = i;
                break;
            }
        }

        return content.Substring(methodStart, Math.Min(methodEnd - methodStart + 1, content.Length - methodStart));
    }

    /// <summary>
    /// Property: No Direct Connection String Usage
    /// 
    /// Verifies that services use the connection factory pattern
    /// instead of creating connections with connection strings directly.
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "end-to-end-testing-verification")]
    [Trait("Property", "Connection Factory Pattern")]
    public bool NoDirectConnectionStringUsage(int fileIndex)
    {
        var actualIndex = Math.Abs(fileIndex) % _serviceFiles.Count;
        var serviceFile = _serviceFiles[actualIndex];
        
        var content = File.ReadAllText(serviceFile);
        var fileName = Path.GetFileName(serviceFile);

        // Check for direct MySqlConnection instantiation with connection string
        var directConnectionPattern = @"new\s+MySqlConnection\s*\(\s*""[^""]+""";
        var directConnectionMatches = Regex.Matches(content, directConnectionPattern);

        if (directConnectionMatches.Count > 0)
        {
            // Allow this in DbConnectionFactory itself
            if (fileName.Contains("DbConnectionFactory") || fileName.Contains("ConnectionFactory"))
            {
                return true;
            }

            foreach (Match match in directConnectionMatches)
            {
                throw new Exception($"✗ {fileName}: Direct connection string usage found at position {match.Index}. Should use connection factory pattern.");
            }
        }

        return true;
    }
}

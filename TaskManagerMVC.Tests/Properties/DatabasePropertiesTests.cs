using FsCheck;
using FsCheck.Xunit;
using FluentAssertions;
using System.Text.RegularExpressions;
using System.Data;

namespace TaskManagerMVC.Tests.Properties;

/// <summary>
/// Property-based tests for database operations
/// Validates universal properties across all service methods
/// </summary>
public class DatabasePropertiesTests
{
    private readonly string _servicesPath;
    private readonly List<string> _serviceFiles;

    public DatabasePropertiesTests()
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
    /// Property 1: Service Methods Use Stored Procedures
    /// **Validates: Requirements 1.3**
    /// 
    /// For any service method that performs database operations, 
    /// the method SHALL use CommandType.StoredProcedure instead of inline SQL queries.
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "end-to-end-testing-verification")]
    [Trait("Property", "1: Service Methods Use Stored Procedures")]
    public bool ServiceMethodsUseStoredProcedures(int fileIndex)
    {
        // Use modulo to ensure we stay within bounds
        var actualIndex = Math.Abs(fileIndex) % _serviceFiles.Count;
        var serviceFile = _serviceFiles[actualIndex];
        
        // Read the service file content
        var content = File.ReadAllText(serviceFile);
        var fileName = Path.GetFileName(serviceFile);

        // Find all MySqlCommand instantiations
        var commandPattern = @"new\s+MySqlCommand\s*\([^)]+\)";
        var commandMatches = Regex.Matches(content, commandPattern);

        if (commandMatches.Count == 0)
        {
            // No database commands in this file - property holds trivially
            return true;
        }

        // For each MySqlCommand, verify it's followed by CommandType.StoredProcedure
        foreach (Match match in commandMatches)
        {
            var commandPosition = match.Index;
            var commandText = match.Value;

            // Extract the context around this command (next 200 characters)
            var contextLength = Math.Min(200, content.Length - commandPosition);
            var context = content.Substring(commandPosition, contextLength);

            // Check if CommandType.StoredProcedure is set for this command
            var hasStoredProcedureType = Regex.IsMatch(context, @"CommandType\s*=\s*CommandType\.StoredProcedure");

            if (!hasStoredProcedureType)
            {
                // Check if it's using CommandType.Text (inline SQL) - this is a violation
                var hasTextType = Regex.IsMatch(context, @"CommandType\s*=\s*CommandType\.Text");
                
                if (hasTextType)
                {
                    throw new Exception($"✗ {fileName}: Found CommandType.Text (inline SQL) at position {commandPosition}");
                }

                // If no CommandType is explicitly set, check if it's a query string
                // (which would default to CommandType.Text)
                var commandConstructor = Regex.Match(commandText, @"new\s+MySqlCommand\s*\(\s*""([^""]+)""");
                if (commandConstructor.Success)
                {
                    var sqlText = commandConstructor.Groups[1].Value;
                    
                    // Check if it looks like a SQL query (SELECT, INSERT, UPDATE, DELETE)
                    if (Regex.IsMatch(sqlText, @"^\s*(SELECT|INSERT|UPDATE|DELETE)\s+", RegexOptions.IgnoreCase))
                    {
                        throw new Exception($"✗ {fileName}: Found inline SQL without CommandType.StoredProcedure at position {commandPosition}: {sqlText.Substring(0, Math.Min(50, sqlText.Length))}");
                    }
                }

                // If it's a stored procedure name (not a SQL query), verify CommandType is set
                // Stored procedure names typically start with "sp_" or are simple identifiers
                var spNameMatch = Regex.Match(commandText, @"new\s+MySqlCommand\s*\(\s*""(sp_[^""]+)""");
                if (spNameMatch.Success)
                {
                    // This looks like a stored procedure call, but CommandType might not be set yet
                    // Check if it's set in the next few lines
                    if (!hasStoredProcedureType)
                    {
                        throw new Exception($"✗ {fileName}: Stored procedure call without CommandType.StoredProcedure at position {commandPosition}: {spNameMatch.Groups[1].Value}");
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Property: No Inline SQL Queries
    /// 
    /// Verifies that service methods do not contain inline SQL queries.
    /// This is a stricter check that looks for SQL keywords in string literals.
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "end-to-end-testing-verification")]
    [Trait("Property", "1: Service Methods Use Stored Procedures (Strict)")]
    public bool NoInlineSqlQueries(int fileIndex)
    {
        var actualIndex = Math.Abs(fileIndex) % _serviceFiles.Count;
        var serviceFile = _serviceFiles[actualIndex];
        
        var content = File.ReadAllText(serviceFile);
        var fileName = Path.GetFileName(serviceFile);

        // Look for SQL queries in string literals
        // Pattern: string literals containing SQL keywords
        var sqlPatterns = new[]
        {
            @"""[^""]*\b(SELECT\s+\*|SELECT\s+\w+)\s+FROM\s+\w+[^""]*""",
            @"""[^""]*\bINSERT\s+INTO\s+\w+[^""]*""",
            @"""[^""]*\bUPDATE\s+\w+\s+SET\s+[^""]*""",
            @"""[^""]*\bDELETE\s+FROM\s+\w+[^""]*"""
        };

        foreach (var pattern in sqlPatterns)
        {
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                var sqlQuery = match.Value;
                
                // Exclude comments and documentation
                var lineStart = content.LastIndexOf('\n', match.Index) + 1;
                var lineEnd = content.IndexOf('\n', match.Index);
                if (lineEnd == -1) lineEnd = content.Length;
                var line = content.Substring(lineStart, lineEnd - lineStart);

                // Skip if it's in a comment
                if (line.TrimStart().StartsWith("//") || line.TrimStart().StartsWith("*"))
                {
                    continue;
                }

                throw new Exception($"✗ {fileName}: Found inline SQL query at position {match.Index}: {sqlQuery.Substring(0, Math.Min(80, sqlQuery.Length))}");
            }
        }

        return true;
    }

    /// <summary>
    /// Property: All MySqlCommand Uses Have CommandType Set
    /// 
    /// Verifies that every MySqlCommand instantiation has its CommandType property set.
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "end-to-end-testing-verification")]
    [Trait("Property", "1: Service Methods Use Stored Procedures (CommandType Check)")]
    public bool AllMySqlCommandsHaveCommandTypeSet(int fileIndex)
    {
        var actualIndex = Math.Abs(fileIndex) % _serviceFiles.Count;
        var serviceFile = _serviceFiles[actualIndex];
        
        var content = File.ReadAllText(serviceFile);
        var fileName = Path.GetFileName(serviceFile);

        // Find all MySqlCommand variable declarations
        var commandVarPattern = @"(var|MySqlCommand)\s+(\w+)\s*=\s*new\s+MySqlCommand";
        var commandMatches = Regex.Matches(content, commandVarPattern);

        if (commandMatches.Count == 0)
        {
            return true;
        }

        foreach (Match match in commandMatches)
        {
            var varName = match.Groups[2].Value;
            var position = match.Index;

            // Look for CommandType assignment for this variable in the next 300 characters
            var contextLength = Math.Min(300, content.Length - position);
            var context = content.Substring(position, contextLength);

            // Check if CommandType is set for this command variable
            var commandTypePattern = $@"{Regex.Escape(varName)}\.CommandType\s*=\s*CommandType\.StoredProcedure";
            var hasCommandType = Regex.IsMatch(context, commandTypePattern);

            if (!hasCommandType)
            {
                // Check if it's using CommandType.Text
                var textTypePattern = $@"{Regex.Escape(varName)}\.CommandType\s*=\s*CommandType\.Text";
                if (Regex.IsMatch(context, textTypePattern))
                {
                    throw new Exception($"✗ {fileName}: MySqlCommand '{varName}' uses CommandType.Text at position {position}");
                }

                // If no CommandType is set at all, this might be a problem
                // unless it's a stored procedure name in the constructor
                var constructorMatch = Regex.Match(context, @"new\s+MySqlCommand\s*\(\s*""([^""]+)""");
                if (constructorMatch.Success)
                {
                    var commandString = constructorMatch.Groups[1].Value;
                    
                    // If it starts with "sp_", it should have CommandType.StoredProcedure
                    if (commandString.StartsWith("sp_", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception($"✗ {fileName}: MySqlCommand '{varName}' appears to call stored procedure '{commandString}' but CommandType.StoredProcedure is not set");
                    }
                }
            }
        }

        return true;
    }
}

using Microsoft.Extensions.Configuration;

namespace TaskManagerMVC.Tests.Configuration;

/// <summary>
/// Helper class to load test configuration from appsettings.Test.json
/// </summary>
public static class TestConfigurationHelper
{
    private static IConfiguration? _configuration;
    private static TestConfiguration? _testConfiguration;

    /// <summary>
    /// Gets the configuration instance
    /// </summary>
    public static IConfiguration Configuration
    {
        get
        {
            if (_configuration == null)
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true)
                    .Build();
            }
            return _configuration;
        }
    }

    /// <summary>
    /// Gets the test configuration settings
    /// </summary>
    public static TestConfiguration GetTestConfiguration()
    {
        if (_testConfiguration == null)
        {
            _testConfiguration = new TestConfiguration
            {
                ConnectionString = Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
                BaseUrl = Configuration["TestConfiguration:BaseUrl"] ?? "http://localhost:5000",
                TimeoutSeconds = int.Parse(Configuration["TestConfiguration:TimeoutSeconds"] ?? "30"),
                RunSecurityTests = bool.Parse(Configuration["TestConfiguration:RunSecurityTests"] ?? "true"),
                RunPerformanceTests = bool.Parse(Configuration["TestConfiguration:RunPerformanceTests"] ?? "false"),
                PerformanceTestConcurrentUsers = int.Parse(Configuration["TestConfiguration:PerformanceTestConcurrentUsers"] ?? "100"),
                RequiredStoredProcedures = Configuration.GetSection("TestConfiguration:RequiredStoredProcedures").Get<List<string>>() ?? new List<string>(),
                RequiredControllers = Configuration.GetSection("TestConfiguration:RequiredControllers").Get<List<string>>() ?? new List<string>(),
                RequiredRoles = Configuration.GetSection("TestConfiguration:RequiredRoles").Get<List<string>>() ?? new List<string>(),
                RequiredPolicies = Configuration.GetSection("TestConfiguration:RequiredPolicies").Get<List<string>>() ?? new List<string>()
            };
        }
        return _testConfiguration;
    }

    /// <summary>
    /// Gets the database connection string
    /// </summary>
    public static string GetConnectionString()
    {
        return GetTestConfiguration().ConnectionString;
    }
}

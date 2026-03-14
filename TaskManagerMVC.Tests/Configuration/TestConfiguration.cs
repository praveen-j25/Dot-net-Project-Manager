namespace TaskManagerMVC.Tests.Configuration;

/// <summary>
/// Configuration settings for test execution
/// </summary>
public class TestConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public bool RunSecurityTests { get; set; } = true;
    public bool RunPerformanceTests { get; set; } = false;
    public int PerformanceTestConcurrentUsers { get; set; } = 100;
    public List<string> RequiredStoredProcedures { get; set; } = new();
    public List<string> RequiredControllers { get; set; } = new();
    public List<string> RequiredRoles { get; set; } = new();
    public List<string> RequiredPolicies { get; set; } = new();
}

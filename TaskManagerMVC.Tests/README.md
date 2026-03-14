# TaskManagerMVC.Tests

This is the comprehensive testing and verification framework for the ASP.NET Core Task Manager MVC application. The framework validates all 8 mandatory technology requirements through automated tests using xUnit, FsCheck for property-based testing, and custom verification components.

## Project Structure

```
TaskManagerMVC.Tests/
├── Configuration/              # Test configuration classes
│   ├── TestConfiguration.cs
│   └── TestConfigurationHelper.cs
├── Infrastructure/             # Infrastructure verification tests
│   └── InfrastructureTests.cs
├── appsettings.Test.json      # Test configuration file
└── README.md                  # This file
```

## Dependencies

The test project includes the following NuGet packages:

- **xUnit** (2.9.3) - Unit testing framework
- **xunit.runner.visualstudio** (3.1.4) - Visual Studio test runner
- **FsCheck** (3.3.2) - Property-based testing library
- **FsCheck.Xunit** (3.3.2) - FsCheck integration with xUnit
- **Moq** (4.20.72) - Mocking framework
- **FluentAssertions** (8.8.0) - Fluent assertion library
- **Microsoft.AspNetCore.Mvc.Testing** (10.0.3) - Integration testing support
- **MySql.Data** (9.6.0) - MySQL database connectivity

## Configuration

The test configuration is stored in `appsettings.Test.json` and includes:

- **ConnectionString**: Database connection string for test database
- **BaseUrl**: Base URL for integration tests
- **TimeoutSeconds**: Default timeout for test operations
- **RunSecurityTests**: Flag to enable/disable security tests
- **RunPerformanceTests**: Flag to enable/disable performance tests
- **PerformanceTestConcurrentUsers**: Number of concurrent users for performance tests
- **RequiredStoredProcedures**: List of stored procedures that must exist
- **RequiredControllers**: List of controllers that must exist
- **RequiredRoles**: List of user roles that must be configured
- **RequiredPolicies**: List of authorization policies that must be configured

## Running Tests

### Run all tests
```bash
dotnet test TaskManagerMVC.Tests
```

### Run tests with detailed output
```bash
dotnet test TaskManagerMVC.Tests --verbosity normal
```

### Run specific test class
```bash
dotnet test TaskManagerMVC.Tests --filter "FullyQualifiedName~InfrastructureTests"
```

### Run tests by category
```bash
dotnet test TaskManagerMVC.Tests --filter "Category=Unit"
```

## Test Categories

The testing framework includes the following test categories:

1. **Infrastructure Tests**: Verify test setup and configuration
2. **Database Verification Tests**: Validate MySQL database and stored procedures
3. **ADO.NET Verification Tests**: Verify ADO.NET usage without Entity Framework
4. **Authentication Tests**: Validate JWT and password hashing
5. **Authorization Tests**: Verify role-based and policy-based authorization
6. **MVC Tests**: Validate ASP.NET Core MVC implementation
7. **Razor Views Tests**: Verify Razor views and UI implementation
8. **Validation & Security Tests**: Validate security measures
9. **CRUD Tests**: Verify CRUD operations for all entities
10. **Integration Tests**: Test end-to-end workflows
11. **Security Tests**: Test vulnerabilities and protections
12. **Performance Tests**: Validate response times and scalability

## Property-Based Testing

The framework uses FsCheck for property-based testing to validate universal correctness properties across many generated inputs. Each property test:

- Runs a minimum of 100 iterations
- Uses FsCheck generators for random test data
- Is tagged with feature name and property number
- References the design document property

Example property test format:
```csharp
[Property]
[Trait("Feature", "end-to-end-testing-verification")]
[Trait("Property", "1: Service Methods Use Stored Procedures")]
public Property ServiceMethodsUseStoredProcedures()
{
    // Property test implementation
}
```

## Test Database Setup

The tests use a separate test database (`task_manager_db_test`) to avoid affecting the development database. To set up the test database:

1. Create the test database:
```sql
CREATE DATABASE task_manager_db_test;
```

2. Run the same schema and stored procedures as the main database
3. Optionally seed with test data

## Continuous Integration

The test suite is designed to run in CI/CD pipelines. Ensure the following:

- Database is accessible from the CI environment
- Connection string is configured correctly
- All required dependencies are installed
- Tests run with appropriate timeout settings

## Verification Report

The testing framework generates a comprehensive verification report that includes:

1. **Executive Summary**: Overall compliance status and test counts
2. **Requirement Verification Results**: Status for each of the 15 requirements
3. **Test Results**: Detailed results for all tests
4. **Security Audit Results**: Security test results with severity ratings
5. **Performance Metrics**: Response times and resource usage
6. **Recommendations**: Issues to fix and improvements to make

## Contributing

When adding new tests:

1. Follow the existing test structure and naming conventions
2. Use FluentAssertions for assertions
3. Tag tests with appropriate categories
4. Document property tests with validation references
5. Ensure tests are isolated and can run in any order
6. Clean up test data after test execution

## Notes

- The infrastructure tests verify that the test setup is working correctly
- Some tests may fail if the database is not accessible
- Security tests may be disabled in certain environments
- Performance tests are disabled by default and should be run separately

# Test BCrypt password verification
# Install BCrypt.Net-Next if not already installed
# dotnet add package BCrypt.Net-Next

$password = "Admin@123"

# These are the three different hashes found in the SQL files:
$hash1 = '$2a$11$rBHXMkvPxZfYGJGvPFqzOeUHq8qMZL.Y9kX.5ZQzQZv8Z5Z5Z5Z5Z'  # task_manager_enhanced.sql
$hash2 = '$2a$11$KGKqLFzuNPr5Tt8H8JFWweQq/kFGEQ8XFlYKZMfN8yf7QjD8V7YY.'  # migrate_simple.sql
$hash3 = '$2a$11$vI8aWBnW3fID.ZQ4/zo1G.q1lRps.9cGLcZEiGDMVr5yUP1KUOYTa'  # COMPLETE_DATABASE_SETUP.sql

Write-Host "Testing password: $password" -ForegroundColor Cyan
Write-Host ""

# Create a simple C# script to test
$code = @"
using System;
using BCrypt.Net;

public class PasswordTester {
    public static void TestHash(string password, string hash, string source) {
        try {
            bool isValid = BCrypt.Verify(password, hash);
            Console.WriteLine($"{source}: {(isValid ? "VALID" : "INVALID")}");
        } catch (Exception ex) {
            Console.WriteLine($"{source}: ERROR - {ex.Message}");
        }
    }
}
"@

Write-Host "Hash 1 (task_manager_enhanced.sql): $hash1"
Write-Host "Hash 2 (migrate_simple.sql): $hash2"
Write-Host "Hash 3 (COMPLETE_DATABASE_SETUP.sql): $hash3"
Write-Host ""
Write-Host "To test these hashes, run the following SQL query to check which hash is in your database:"
Write-Host ""
Write-Host "SELECT email, password FROM users WHERE email = 'admin@taskmanager.com';" -ForegroundColor Yellow

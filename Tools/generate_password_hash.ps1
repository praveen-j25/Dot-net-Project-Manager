# Generate BCrypt Password Hash for Admin@123
# Run this in PowerShell to get a fresh hash

Write-Host "Generating BCrypt hash for password: Admin@123" -ForegroundColor Yellow
Write-Host ""

# Create a simple C# program to generate the hash
$code = @"
using System;
using BCrypt.Net;

public class PasswordHasher
{
    public static void Main()
    {
        string password = "Admin@123";
        string hash = BCrypt.HashPassword(password, 11);
        Console.WriteLine("Password: " + password);
        Console.WriteLine("BCrypt Hash:");
        Console.WriteLine(hash);
        Console.WriteLine("");
        Console.WriteLine("SQL Command:");
        Console.WriteLine("UPDATE users SET password = '" + hash + "' WHERE email = 'admin@taskmanager.com';");
    }
}
"@

Write-Host "To generate a fresh hash:" -ForegroundColor Cyan
Write-Host "1. Open your C# project" -ForegroundColor White
Write-Host "2. Add this code to a test file" -ForegroundColor White
Write-Host "3. Run it to get the hash" -ForegroundColor White
Write-Host ""
Write-Host "OR use this SQL to reset password:" -ForegroundColor Cyan
Write-Host ""
Write-Host "mysql -u root -p < database\fix_admin_password.sql" -ForegroundColor Green
Write-Host ""

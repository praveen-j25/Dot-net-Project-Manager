// Quick test to generate correct BCrypt hash
// Run this: dotnet script TestPasswordHash.cs

using System;

// Simulating BCrypt (you'll need to run this in your actual project)
class Program
{
    static void Main()
    {
        string password = "Admin@123";
        
        // This will generate the hash using BCrypt.Net
        string hash = BCrypt.Net.BCrypt.HashPassword(password);
        
        Console.WriteLine("Password: " + password);
        Console.WriteLine("Hash: " + hash);
        Console.WriteLine("");
        Console.WriteLine("SQL Command to update:");
        Console.WriteLine($"UPDATE users SET password = '{hash}' WHERE email = 'admin@taskmanager.com';");
        
        // Test verification
        bool isValid = BCrypt.Net.BCrypt.Verify(password, hash);
        Console.WriteLine("");
        Console.WriteLine("Verification test: " + (isValid ? "PASS" : "FAIL"));
    }
}

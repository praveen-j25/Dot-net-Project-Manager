using System;
using BCrypt.Net;

class Program
{
    static void Main()
    {
        var password = "Admin@123";
        
        // Hash from fix_admin_password.sql (the one we just applied)
        var hash = "$2a$11$KGKqLFzuNPr5Tt8H8JFWweQq/kFGEQ8XFlYKZMfN8yf7QjD8V7YY.";
        
        Console.WriteLine("Testing BCrypt Password Verification");
        Console.WriteLine("=====================================");
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Hash: {hash}");
        Console.WriteLine();
        
        try 
        {
            bool isValid = BCrypt.Verify(password, hash);
            Console.WriteLine($"Result: {(isValid ? "VALID - Password matches!" : "INVALID - Password does NOT match!")}");
            
            if (!isValid) 
            {
                Console.WriteLine();
                Console.WriteLine("Generating correct hash for 'Admin@123':");
                var correctHash = BCrypt.HashPassword("Admin@123", 11);
                Console.WriteLine($"New Hash: {correctHash}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
    }
}

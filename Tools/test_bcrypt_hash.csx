#!/usr/bin/env dotnet-script
#r "nuget: BCrypt.Net-Next, 4.0.3"

using BCrypt.Net;

var password = "Admin@123";

// Hash from migrate_simple.sql (the one being used)
var hash = "$2a$11$KGKqLFzuNPr5Tt8H8JFWweQq/kFGEQ8XFlYKZMfN8yf7QjD8V7YY.";

Console.WriteLine("Testing BCrypt Password Verification");
Console.WriteLine("=====================================");
Console.WriteLine($"Password: {password}");
Console.WriteLine($"Hash: {hash}");
Console.WriteLine();

try 
{
    bool isValid = BCrypt.Verify(password, hash);
    Console.WriteLine($"Result: {(isValid ? "✓ VALID - Password matches!" : "✗ INVALID - Password does NOT match!")}");
    
    if (!isValid) 
    {
        Console.WriteLine();
        Console.WriteLine("Generating correct hash for 'Admin@123':");
        var correctHash = BCrypt.HashPassword("Admin@123");
        Console.WriteLine($"New Hash: {correctHash}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
}

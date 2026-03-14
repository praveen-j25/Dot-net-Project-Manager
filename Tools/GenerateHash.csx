#!/usr/bin/env dotnet-script
#r "nuget: BCrypt.Net-Next, 4.0.3"

using BCrypt.Net;

var password = "Admin@123";
var hash = BCrypt.HashPassword(password);

Console.WriteLine("Password: " + password);
Console.WriteLine("Hash: " + hash);
Console.WriteLine("");
Console.WriteLine("Run this SQL:");
Console.WriteLine($"UPDATE users SET password = '{hash}' WHERE email = 'admin@taskmanager.com';");

using Microsoft.AspNetCore.Mvc;

namespace TaskManagerMVC.Controllers;

public class TestController : Controller
{
    [HttpGet("/test/hash")]
    public IActionResult TestHash(string password = "Admin@123")
    {
        // Generate a fresh hash
        var freshHash = BCrypt.Net.BCrypt.HashPassword(password, 11);
        
        // Hash from fix_admin_password.sql (currently in database)
        var currentDbHash = "$2a$11$KGKqLFzuNPr5Tt8H8JFWweQq/kFGEQ8XFlYKZMfN8yf7QjD8V7YY.";
        
        // Old hash from COMPLETE_DATABASE_SETUP.sql
        var oldHash = "$2a$11$vI8aWBnW3fID.ZQ4/zo1G.q1lRps.9cGLcZEiGDMVr5yUP1KUOYTa";
        
        // Test both hashes
        var isCurrentValid = BCrypt.Net.BCrypt.Verify(password, currentDbHash);
        var isOldValid = BCrypt.Net.BCrypt.Verify(password, oldHash);
        
        return Json(new
        {
            password = password,
            freshHash = freshHash,
            currentDbHash = currentDbHash,
            currentDbHashValid = isCurrentValid,
            oldHash = oldHash,
            oldHashValid = isOldValid,
            message = isCurrentValid ? "✓ Current DB hash is VALID!" : "✗ Current DB hash is INVALID!",
            recommendation = isCurrentValid ? "Hash is correct. Check if app is reading from correct database." : "Need to update database hash."
        });
    }
}

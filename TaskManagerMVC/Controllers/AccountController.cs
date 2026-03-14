using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagerMVC.Models;
using TaskManagerMVC.Services;
using TaskManagerMVC.ViewModels;

namespace TaskManagerMVC.Controllers;

public class AccountController : Controller
{
    private readonly AuthService _auth;
    private readonly NotificationService _notificationService;

    public AccountController(AuthService auth, NotificationService notificationService)
    {
        _auth = auth;
        _notificationService = notificationService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            // Redirect based on role
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Employee";
            return role switch
            {
                "Administrator" or "Admin" => RedirectToAction("Index", "Admin"),
                "Manager" => RedirectToAction("Index", "Manager"),
                _ => RedirectToAction("Index", "Employee")
            };
        }
        
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM model)
    {
        if (!ModelState.IsValid)
            return View(model);

        Models.User? user = null;
        try
        {
            user = await _auth.LoginAsync(model.Email, model.Password);
            
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Login error: {ex.Message}");
            return View(model);
        }

        var roleName = user.Role?.Name ?? "Employee";
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, roleName),
            new("FirstName", user.FirstName),
            new("RoleId", user.RoleId.ToString()),
            new("JobTitle", user.JobTitle ?? ""),
            new("ProfileImage", user.ProfileImage ?? "")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties { IsPersistent = model.RememberMe };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
            new ClaimsPrincipal(identity), properties);

        // Redirect based on role
        return user.RoleId switch
        {
            1 => RedirectToAction("Index", "Admin"),    // Admin
            2 => RedirectToAction("Index", "Manager"),  // Manager
            _ => RedirectToAction("Index", "Employee")  // Employee
        };
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        
        var model = new RegisterVM();
        await PopulateRegisterDropdowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVM model)
    {
        // SECURITY: Force all public registrations to Employee role (RoleId = 3)
        // Only admins can create Admin/Manager accounts through the admin panel
        model.RoleId = 3;
        
        if (!ModelState.IsValid)
        {
            await PopulateRegisterDropdowns(model);
            return View(model);
        }

        var (success, message) = await _auth.RegisterAsync(model);

        if (!success)
        {
            ModelState.AddModelError("", message);
            await PopulateRegisterDropdowns(model);
            return View(model);
        }

        TempData["Success"] = message; // Will show "Registration request submitted! Please wait for admin approval."
        
        // Notify admins
        await _notificationService.NotifyAdminsAsync(
            "New Registration Request", 
            $"New user registration request from {model.Email}", 
            "registration_request"
        );

        return RedirectToAction(nameof(Login));
    }

    private async Task PopulateRegisterDropdowns(RegisterVM model)
    {
        var departments = await _auth.GetDepartmentsAsync();
        model.Departments = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            departments.Select(d => new { Id = d.Id, Name = d.Name }), "Id", "Name");
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var token = await _auth.CreatePasswordResetTokenAsync(model.Email);

        if (token != null)
        {
            TempData["Success"] = "Reset link sent to your email.";
            return RedirectToAction(nameof(ResetPassword), new { token });
        }

        ModelState.AddModelError("", "Email not found");
        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPassword(string token)
    {
        if (string.IsNullOrEmpty(token))
            return RedirectToAction(nameof(ForgotPassword));

        return View(new ResetPasswordVM { Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (await _auth.ResetPasswordAsync(model.Token, model.Password))
        {
            TempData["Success"] = "Password reset successful!";
            return RedirectToAction(nameof(Login));
        }

        ModelState.AddModelError("", "Invalid or expired token");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        
        // Provide helpful message based on user's role
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
            ViewData["Message"] = $"You don't have permission to access this resource. Your current role is: {role}";
        }
        else
        {
            ViewData["Message"] = "You need to be logged in to access this resource.";
        }
        
        return View();
    }
}



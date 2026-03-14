using Microsoft.AspNetCore.Mvc;

namespace TaskManagerMVC.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return User.Identity?.IsAuthenticated == true 
            ? RedirectToAction("Index", "Dashboard") 
            : RedirectToAction("Login", "Account");
    }

    public IActionResult Error() => View();
}

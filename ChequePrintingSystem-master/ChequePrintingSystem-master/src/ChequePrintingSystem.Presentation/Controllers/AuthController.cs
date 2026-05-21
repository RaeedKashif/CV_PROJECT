using ChequePrintingSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChequePrintingSystem.Presentation.Controllers;

public class AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View();
        }

        var result = await signInManager.PasswordSignInAsync(user, password, true, true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View();
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();
}

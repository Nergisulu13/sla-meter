using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SlaMonitor.Auth.Models;

namespace SlaMonitor.Auth.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<AuthUser> _signInManager;
        private readonly UserManager<AuthUser> _userManager;

        public AccountController(
            SignInManager<AuthUser> signInManager,
            UserManager<AuthUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet("/account/login")]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost("/account/login")]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user is null)
            {
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.Error = "Kullanıcı bulunamadı.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, true, false);
            if (!result.Succeeded)
            {
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
                return View();
            }

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return Redirect(returnUrl);

            return Redirect("/");
        }

        [HttpGet("/account/logout")]
        public async Task<IActionResult> Logout(string? returnUrl = null)
        {
    await _signInManager.SignOutAsync();

        if (!string.IsNullOrWhiteSpace(returnUrl))
        return Redirect($"/account/login?returnUrl={Uri.EscapeDataString(returnUrl)}");

       return Redirect("/account/login");
      }
    }
}
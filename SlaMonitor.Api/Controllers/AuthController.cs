using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SlaMonitor.Api.Controllers
{
    [ApiController]
    public class AuthController : Controller
    {
        [HttpGet("/login")]
        public async Task<IActionResult> Login([FromQuery] string returnUrl = "/")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Nergis")
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            return Redirect(returnUrl);
        }

        [HttpGet("/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect("/");
        }
    }
}
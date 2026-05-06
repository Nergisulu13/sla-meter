using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SlaMonitor.Auth.Data;
using SlaMonitor.Auth.Models;

namespace SlaMonitor.Auth.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<AuthUser> _signInManager;
        private readonly UserManager<AuthUser> _userManager;
        private readonly AuthDbContext _dbContext;

        private static readonly List<SelectListItem> FallbackTenantOptions = new()
        {
            new SelectListItem { Value = "11111111-1111-1111-1111-111111111111", Text = "Eclit" },
            new SelectListItem { Value = "22222222-2222-2222-2222-222222222222", Text = "Paris" },
            new SelectListItem { Value = "33333333-3333-3333-3333-333333333333", Text = "Huawei" },
            new SelectListItem { Value = "44444444-4444-4444-4444-444444444444", Text = "Ohio" },
            new SelectListItem { Value = "55555555-5555-5555-5555-555555555555", Text = "UAE" },
            new SelectListItem { Value = "66666666-6666-6666-6666-666666666666", Text = "Preprod Ireland" }
        };

        public AccountController(
            SignInManager<AuthUser> signInManager,
            UserManager<AuthUser> userManager,
            AuthDbContext dbContext)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        [HttpGet("/account/login")]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            HttpContext.Session.Remove("selected_tenant_id");
            HttpContext.Session.Remove("selected_tenant_name");

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl,
                TenantOptions = await GetTenantOptionsAsync()
            };

            return View(model);
        }

        [HttpPost("/account/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            model.TenantOptions = await GetTenantOptionsAsync();

            ViewData["Debug"] =
                $"TenantId='{model.TenantId}', UserName='{model.UserName}', PasswordLength={(model.Password?.Length ?? 0)}";

            if (!ModelState.IsValid)
                return View(model);

            var selectedTenant = await _dbContext.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id.ToString() == model.TenantId && x.IsActive);

            if (selectedTenant == null)
            {
                var fallbackTenant = FallbackTenantOptions.FirstOrDefault(x => x.Value == model.TenantId);

                if (fallbackTenant == null)
                {
                    ModelState.AddModelError(nameof(model.TenantId), "Geçerli bir tenant seçiniz.");
                    ViewData["Debug"] = $"Tenant bulunamadı. Gelen TenantId='{model.TenantId}'";
                    return View(model);
                }

                selectedTenant = new Tenant
                {
                    Id = Guid.Parse(fallbackTenant.Value),
                    Name = fallbackTenant.Text,
                    DisplayName = fallbackTenant.Text,
                    IsActive = true
                };
            }

            var user = await _userManager.FindByNameAsync(model.UserName?.Trim() ?? "");

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
                ViewData["Debug"] = $"User bulunamadı. Gelen UserName='{model.UserName}'";
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                isPersistent: model.RememberMe,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
                ViewData["Debug"] = $"PasswordSignIn başarısız. User='{user.UserName}'";
                return View(model);
            }

            HttpContext.Session.SetString("selected_tenant_id", selectedTenant.Id.ToString());
            HttpContext.Session.SetString(
                "selected_tenant_name",
                string.IsNullOrWhiteSpace(selectedTenant.DisplayName)
                    ? selectedTenant.Name
                    : selectedTenant.DisplayName);

            ViewData["Debug"] = $"Login başarılı. User='{user.UserName}', Tenant='{selectedTenant.Name}'";

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return Redirect("http://localhost:4200/incidents");
        }

        [HttpGet("/account/logout")]
        public async Task<IActionResult> Logout(string? returnUrl = null)
        {
            await _signInManager.SignOutAsync();

            HttpContext.Session.Remove("selected_tenant_id");
            HttpContext.Session.Remove("selected_tenant_name");

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect($"/account/login?returnUrl={Uri.EscapeDataString(returnUrl)}");

            return Redirect("/account/login");
        }

        private async Task<List<SelectListItem>> GetTenantOptionsAsync()
        {
            var dbTenants = await _dbContext.Tenants
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(x.DisplayName) ? x.Name : x.DisplayName
                })
                .ToListAsync();

            if (dbTenants.Any())
                return dbTenants;

            return FallbackTenantOptions;
        }
    }
}
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using SlaMonitor.Auth.Data;
using SlaMonitor.Auth.Models;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SlaMonitor.Auth.Controllers
{
    public class AuthorizationController : Controller
    {
        private readonly UserManager<AuthUser> _userManager;
        private readonly SignInManager<AuthUser> _signInManager;
        private readonly AuthDbContext _dbContext;

        public AuthorizationController(
            UserManager<AuthUser> userManager,
            SignInManager<AuthUser> signInManager,
            AuthDbContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = dbContext;
        }

        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        public async Task<IActionResult> Authorize()
        {
            if (User.Identity is not { IsAuthenticated: true })
            {
                var returnUrl = Request.PathBase + Request.Path + Request.QueryString;
                return Challenge(new AuthenticationProperties
                {
                    RedirectUri = returnUrl
                });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

                var returnUrl = Request.PathBase + Request.Path + Request.QueryString;
                return Challenge(new AuthenticationProperties
                {
                    RedirectUri = returnUrl
                });
            }

            var principal = await _signInManager.CreateUserPrincipalAsync(user);
            var identity = (ClaimsIdentity?)principal.Identity;

            if (identity == null)
            {
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

                var returnUrl = Request.PathBase + Request.Path + Request.QueryString;
                return Challenge(new AuthenticationProperties
                {
                    RedirectUri = returnUrl
                });
            }

            principal.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user));
            principal.SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user) ?? string.Empty);

            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            var selectedTenantId = HttpContext.Session.GetString("selected_tenant_id");
            var selectedTenantName = HttpContext.Session.GetString("selected_tenant_name");

            if (string.IsNullOrWhiteSpace(selectedTenantId) || string.IsNullOrWhiteSpace(selectedTenantName))
            {
                return RedirectToAction("Login", "Account", new
                {
                    returnUrl = Request.PathBase + Request.Path + Request.QueryString
                });
            }

            var tenantExists = await _dbContext.Tenants.AnyAsync(x =>
                x.Id.ToString() == selectedTenantId && x.IsActive);

            if (!tenantExists)
            {
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

                return RedirectToAction("Login", "Account", new
                {
                    returnUrl = Request.PathBase + Request.Path + Request.QueryString
                });
            }

            principal.SetClaim("tenant_id", selectedTenantId);
            principal.SetClaim("tenant_name", selectedTenantName);

            if (roles.Contains("SuperAdmin"))
            {
                principal.SetClaim("is_superadmin", "true");
            }

            principal.SetScopes(new[]
            {
                Scopes.OpenId,
                Scopes.Profile,
                Scopes.OfflineAccess,
                "incidents_api"
            });

            principal.SetResources("resource_server");

            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim));
            }

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpPost("~/connect/token")]
        public async Task<IActionResult> Exchange()
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (result.Principal is null)
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            foreach (var claim in result.Principal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim));
            }

            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        private static IEnumerable<string> GetDestinations(Claim claim)
        {
            switch (claim.Type)
            {
                case Claims.Name:
                case Claims.Subject:
                case "tenant_id":
                case "tenant_name":
                    return new[]
                    {
                        Destinations.AccessToken,
                        Destinations.IdentityToken
                    };

                case ClaimTypes.Role:
                case "is_superadmin":
                    return new[]
                    {
                        Destinations.AccessToken
                    };

                default:
                    return new[]
                    {
                        Destinations.AccessToken
                    };
            }
        }
    }
}
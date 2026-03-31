using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using SlaMonitor.Auth.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SlaMonitor.Auth.Controllers
{
    public class AuthorizationController : Controller
    {
        private readonly UserManager<AuthUser> _userManager;
        private readonly SignInManager<AuthUser> _signInManager;

        public AuthorizationController(
            UserManager<AuthUser> userManager,
            SignInManager<AuthUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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

            principal.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user));
            principal.SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user) ?? "");

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

            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        private static IEnumerable<string> GetDestinations(System.Security.Claims.Claim claim)
        {
            switch (claim.Type)
            {
                case Claims.Name:
                case Claims.Subject:
                    return new[]
                    {
                        Destinations.AccessToken,
                        Destinations.IdentityToken
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
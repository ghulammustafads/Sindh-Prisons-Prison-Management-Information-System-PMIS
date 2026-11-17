using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrisonManagementSystem.Authorization;
using PrisonManagementSystem.Services;
using PrisonManagementSystem.ViewModels;

namespace PrisonManagementSystem.Controllers;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAuthService authService, ILogger<AccountController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, bool loggedOut = false)
    {
        if (loggedOut || Request.Query.ContainsKey("loggedOut"))
        {
            // Explicit sign out: clear cookies across all possible SameSite and Secure flags
            Response.Cookies.Delete("PMIS_AUTH_TOKEN", new CookieOptions { Path = "/" });
            Response.Cookies.Delete("PMIS_AUTH_TOKEN", new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax });
            Response.Cookies.Delete("PMIS_AUTH_TOKEN", new CookieOptions { Path = "/", SameSite = SameSiteMode.None, Secure = true });
            Response.Cookies.Delete(".SindhPrisons.PMIS.Auth", new CookieOptions { Path = "/" });
            Response.Cookies.Delete(".SindhPrisons.PMIS.Auth", new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax });
            Response.Cookies.Delete(".SindhPrisons.PMIS.Auth", new CookieOptions { Path = "/", SameSite = SameSiteMode.None, Secure = true });
            return View(new LoginViewModel());
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var assignedController = RoleConstants.GetAssignedController(role);
            if (!string.IsNullOrEmpty(assignedController))
            {
                return RedirectToAction("Index", assignedController);
            }
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.AuthenticateAsync(model.Username, model.Password);
        if (!result.Succeeded || result.Account == null || result.Staff == null || result.RoleName == null)
        {
            model.ErrorMessage = result.ErrorMessage ?? "Invalid username or password.";
            return View(model);
        }

        // Establish secure cookie claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.Account.AccountID.ToString()),
            new(ClaimTypes.Name, result.Account.Username),
            new(ClaimTypes.GivenName, result.Staff.FullName),
            new(ClaimTypes.Role, result.RoleName),
            new("StaffID", result.Staff.StaffID.ToString()),
            new("OfficialRank", result.Staff.OfficialRank ?? string.Empty)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Issue secure PMIS auth token for iframe persistence
        var token = AuthTokenService.CreateToken(
            result.Account.Username,
            result.RoleName,
            result.Staff.StaffID,
            result.Staff.FullName,
            result.Staff.OfficialRank ?? string.Empty);

        // Set fallback cookie as SameSite=Lax
        Response.Cookies.Append("PMIS_AUTH_TOKEN", token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddHours(8)
        });

        _logger.LogInformation("User {Username} logged in with role {RoleName}.", result.Account.Username, result.RoleName);

        // Strict role-to-page redirection
        var destinationController = result.AssignedController ?? "Dashboard";
        return Redirect($"/{destinationController}?t={Uri.EscapeDataString(token)}");
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();

        Response.Cookies.Delete("PMIS_AUTH_TOKEN", new CookieOptions { Path = "/" });
        Response.Cookies.Delete("PMIS_AUTH_TOKEN", new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax });
        Response.Cookies.Delete("PMIS_AUTH_TOKEN", new CookieOptions { Path = "/", SameSite = SameSiteMode.None, Secure = true });
        Response.Cookies.Delete(".SindhPrisons.PMIS.Auth", new CookieOptions { Path = "/" });
        Response.Cookies.Delete(".SindhPrisons.PMIS.Auth", new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax });
        Response.Cookies.Delete(".SindhPrisons.PMIS.Auth", new CookieOptions { Path = "/", SameSite = SameSiteMode.None, Secure = true });

        HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        return Redirect("/Account/Login?loggedOut=true");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

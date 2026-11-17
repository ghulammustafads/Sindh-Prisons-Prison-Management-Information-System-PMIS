using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PrisonManagementSystem.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class EnforceAssignedRolePageAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _requiredRole;

    public EnforceAssignedRolePageAttribute(string requiredRole)
    {
        _requiredRole = requiredRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrWhiteSpace(userRole) ||
            !string.Equals(userRole, _requiredRole, StringComparison.OrdinalIgnoreCase))
        {
            // Unauthorized access to another role's page - redirect to AccessDenied
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}

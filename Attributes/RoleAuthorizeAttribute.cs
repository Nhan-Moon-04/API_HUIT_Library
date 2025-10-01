using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace HUIT_Library.Attributes
{
    public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public RoleAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Ki?m tra xem user ?ã ??ng nh?p ch?a
            if (!context.HttpContext.User.Identity!.IsAuthenticated)
            {
                context.Result = new UnauthorizedObjectResult(new { message = "B?n c?n ??ng nh?p ?? truy c?p!" });
                return;
            }

            // L?y role t? token
            var userRole = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (string.IsNullOrEmpty(userRole) || !_roles.Contains(userRole))
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
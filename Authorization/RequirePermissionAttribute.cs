using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AtharERP_System.Authorization
{
    // استخدم: [RequirePermission("Projects.View")] فوق أي Action أو Controller
    public class RequirePermissionAttribute : TypeFilterAttribute
    {
        public RequirePermissionAttribute(string permissionName) : base(typeof(RequirePermissionFilter))
        {
            Arguments = new object[] { permissionName };
        }
    }

    public class RequirePermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permissionName;
        private readonly PermissionService _permissionService;

        public RequirePermissionFilter(string permissionName, PermissionService permissionService)
        {
            _permissionName = permissionName;
            _permissionService = permissionService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;

            if (httpContext.User.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                var returnUrl = httpContext.Request.Path + httpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = returnUrl.ToString() });
                return;
            }

            var hasPermission = await _permissionService.HasPermissionAsync(httpContext.User, _permissionName);
            if (!hasPermission)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }
    }
}
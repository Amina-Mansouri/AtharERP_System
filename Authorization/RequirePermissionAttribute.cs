using AtharERP_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AtharERP_System.Authorization
{
    // استخدم: [RequirePermission("Projects.Edit")] أو [RequirePermission("Projects.Edit", "Projects.Stages.Manage")]
    // (يكفي أن يملك المستخدم واحدة من الصلاحيات المذكورة)
    public class RequirePermissionAttribute : TypeFilterAttribute
    {
        public RequirePermissionAttribute(params string[] permissionNames) : base(typeof(RequirePermissionFilter))
        {
            Arguments = new object[] { permissionNames };
        }
    }

    public class RequirePermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string[] _permissionNames;
        private readonly PermissionService _permissionService;

        public RequirePermissionFilter(string[] permissionNames, PermissionService permissionService)
        {
            _permissionNames = permissionNames;
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

            foreach (var permissionName in _permissionNames)
            {
                if (await _permissionService.HasPermissionAsync(httpContext.User, permissionName))
                    return;
            }

            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
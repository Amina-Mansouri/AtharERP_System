using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AtharERP_System.Services
{
    // يحافظ على التبويب الحالي (?tab=pane-x) عند إعادة التوجيه لصفحة فيها تبويبات
    public static class ControllerExtensions
    {
        public static IActionResult RedirectKeepingTab(this Controller controller, string actionName, object? routeValues = null)
        {
            var values = new RouteValueDictionary(routeValues);
            var tab = controller.Request.Query["tab"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tab))
                values["tab"] = tab;
            return controller.RedirectToAction(actionName, values);
        }

        public static IActionResult RedirectKeepingTab(this Controller controller, string actionName, string controllerName, object? routeValues = null)
        {
            var values = new RouteValueDictionary(routeValues);
            var tab = controller.Request.Query["tab"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tab))
                values["tab"] = tab;
            return controller.RedirectToAction(actionName, controllerName, values);
        }
    }
}
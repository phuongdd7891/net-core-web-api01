using AdminWeb.Models.Response;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;

namespace AdminWeb.Helpers
{
    public static class HtmlExt
    {
        public static bool IsSystemUsr(this IHtmlHelper helper)
        {
            var userData = helper.TempData["Profile"] as string;
            if (!string.IsNullOrEmpty(userData))
            {
                helper.TempData.Keep("Profile");
                var profile = JsonConvert.DeserializeObject<AdminProfile>(userData);
                return profile?.IsSystem ?? false;
            }
            return false;
        }

        public static string IsSelected(this IHtmlHelper helper, string? controller = null, string? action = null, string? cssClass = null)
        {
            if (string.IsNullOrEmpty(cssClass)) cssClass = "active";
            var currentAction = Convert.ToString(helper.ViewContext.RouteData.Values["action"]);
            var currentController = Convert.ToString(helper.ViewContext.RouteData.Values["controller"]);
            if (string.IsNullOrEmpty(controller)) controller = currentController;
            if (string.IsNullOrEmpty(action)) action = currentAction;
            return controller?.ToLower() == currentController?.ToLower() && action?.ToLower() == currentAction?.ToLower() ? cssClass : string.Empty;
        }
    }
}

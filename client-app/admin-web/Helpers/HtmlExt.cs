using AdminWeb.Models.Response;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    }
}

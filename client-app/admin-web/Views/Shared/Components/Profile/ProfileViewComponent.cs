using AdminWeb.Models;
using AdminWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

namespace AdminWeb.Views.Shared.Components.Profile
{
    public class ProfileViewComponent : ViewComponent
    {
        private readonly OperationService _opService;
        public ProfileViewComponent(OperationService operationService)
        {
            _opService = operationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity!.IsAuthenticated)
            {
                var profile = await _opService.GetProfile();
                TempData["Profile"] = JsonConvert.SerializeObject(profile.Data);
            }
            return View();
        }
    }
}

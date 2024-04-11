using AdminWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AdminWeb.Views.Shared.Components.Customer
{
    public class CustomerViewComponent: ViewComponent
    {
        private readonly OperationService _opService;
        public CustomerViewComponent(OperationService operationService)
        {
            _opService = operationService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string model, string? value)
        {
            if (User.Identity!.IsAuthenticated)
            {
                var customers = await _opService.GetCustomers();
                TempData["Customers"] = JsonConvert.SerializeObject(customers.Data);
            }
            ViewBag.value = value;
            return View("", model);
        }
    }
}

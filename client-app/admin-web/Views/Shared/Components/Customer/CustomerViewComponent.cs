using AdminWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AdminWeb.Views.Shared.Components.Customer
{
    public class CustomerViewComponent: ViewComponent
    {
        private readonly CustomerService _customerService;
        public CustomerViewComponent(CustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string model, string? value)
        {
            if (User.Identity!.IsAuthenticated)
            {
                var customers = await _customerService.GetCustomers();
                TempData["Customers"] = JsonConvert.SerializeObject(customers.Data);
            }
            ViewBag.value = value;
            return View("", model);
        }
    }
}

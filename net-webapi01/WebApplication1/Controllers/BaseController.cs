using IdentityMongo.Models;
using Microsoft.AspNetCore.Mvc;
using WebApi.Services;

namespace WebApi.Controllers;

public class BaseController: ControllerBase
{
    private ApiKeyService? _apiKeyService;
    protected ApiKeyService apiKeyService => _apiKeyService ?? (_apiKeyService = HttpContext.RequestServices.GetService<ApiKeyService>())!;

    public async Task<ApplicationUser?> GetRequestUser()
    {
        return await apiKeyService.GetRequestUser(Request);
    }
}
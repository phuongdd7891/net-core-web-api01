using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Gateway.Models;

public class CustomActionConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        using var provider = MD5.Create();
        var actionId = $"{action.Controller.ControllerName}_{action.ActionName}";
        byte[] hash = provider.ComputeHash(Encoding.UTF8.GetBytes(actionId));
        action.Properties["ActionId"] = new Guid(hash).ToString();
    }
}

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public static class Helpers
{
    public static JsonResult GetJsonResult(DataResponse<string> data)
    {
        var jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };
        return new JsonResult(data, jsonSettings)
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };
    }
}
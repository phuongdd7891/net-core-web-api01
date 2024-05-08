using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public static class Helpers
{
    private static Dictionary<string, string> ErrDescriptions = new Dictionary<string, string>
    {
        {"DuplicateRoleName", "Existed role name '{0}'"}
    };

    public static JsonResult GetUnauthorizedResult(DataResponse<string> data)
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

    public static T? DeepCopy<T>(this T self)
    {
        var serialized = JsonConvert.SerializeObject(self);
        return JsonConvert.DeserializeObject<T>(serialized);
    }

    public static string ToErrDescription(this string self)
    {
        return ErrDescriptions.ContainsKey(self) ? ErrDescriptions[self] : self;
    }

    public static string ToErrDescription(this string self, params string[] values)
    {
        return ErrDescriptions.ContainsKey(self) ? string.Format(ErrDescriptions[self], values) : self;
    }
}
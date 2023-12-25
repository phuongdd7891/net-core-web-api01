using System.Net;
using System.Text.Json;

public class ErrorStatuses
{
    private static DataResponse<string> GetErrorResponse(int code, string message) => new DataResponse<string> {
        Code = code,
        Data = message,
    };

    public static void ThrowInternalErr(string message, bool condition)
    {
        if (condition)
        {
            throw new Exception(JsonSerializer.Serialize(GetErrorResponse((int)HttpStatusCode.InternalServerError, message)));
        }
    }

    public static void ThrowNotFound(string message, bool condition)
    {
        if (condition)
        {
            throw new Exception(JsonSerializer.Serialize(GetErrorResponse((int)HttpStatusCode.NotFound, message)));
        }
    }

    public static void ThrowBadRequest(string message, bool condition)
    {
        if (condition)
        {
            throw new Exception(JsonSerializer.Serialize(GetErrorResponse((int)HttpStatusCode.BadRequest, message)));
        }
    }
}
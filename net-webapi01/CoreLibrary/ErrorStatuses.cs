using System.Net;
using System.Text.Json;

public class ErrorStatusResponse
{
    public int StatusCode { get; set; }
    public DataResponse<string>? Data { get; set; }
}

public class ErrorStatuses
{
    private static ErrorStatusResponse GetErrorResponse(int statusCode, string errCode, string message) => new ErrorStatusResponse
    {
        StatusCode = statusCode,
        Data = new DataResponse<string>
        {
            Code = errCode,
            Data = message,
        }
    };

    public static void ThrowInternalErr(string message, bool condition, string errCode = "")
    {
        if (condition)
        {
            throw new Exception(JsonSerializer.Serialize(GetErrorResponse((int)HttpStatusCode.InternalServerError, string.IsNullOrEmpty(errCode) ? DataResponseCode.IternalError.ToString() : errCode, message)));
        }
    }

    public static void ThrowNotFound(string message, bool condition)
    {
        if (condition)
        {
            throw new Exception(JsonSerializer.Serialize(GetErrorResponse((int)HttpStatusCode.NotFound, DataResponseCode.NotFound.ToString(), message)));
        }
    }

    public static void ThrowBadRequest(string message, bool condition)
    {
        if (condition)
        {
            throw new Exception(JsonSerializer.Serialize(GetErrorResponse((int)HttpStatusCode.BadRequest, DataResponseCode.InvalidRequest.ToString(), message)));
        }
    }
}
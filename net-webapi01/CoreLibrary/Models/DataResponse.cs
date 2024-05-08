public class DataResponse<T> {
    public string Code { get; set; } = DataResponseCode.Ok.ToString();
    public T? Data { get; set; }
}

public enum DataResponseCode
{
    Ok = 1,
    IternalError = 2,
    NotFound = 3,
    InvalidRequest = 4,
    Unauthorized = 5,
    EmailNotConfirm = 6,
    TokenInvalid = 7,
    TokenExpired = 8
}

public class DataResponse: DataResponse<string>
{ }
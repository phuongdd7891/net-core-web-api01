public class DataResponse<T> {
    public int Code { get; set; } = 200;
    public T? Data { get; set; }
}
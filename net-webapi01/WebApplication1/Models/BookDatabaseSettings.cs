namespace WebApi.Models;

public class BookDatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;
    public string AdminConnectionString { get; set; } = string.Empty;
    public string AdminDatabaseName { get; set; } = string.Empty;
}
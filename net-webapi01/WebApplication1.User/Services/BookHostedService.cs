using WebApplication1.User.Services;

public class BookHostedService : IHostedService
{
    private readonly BookService _bookService;
    private readonly FileService _fileService;
    private readonly string maxCountFilePath = Path.Combine("./", "maxCloneCount.txt");
    public BookHostedService(
        BookService bookService,
        FileService fileService
    )
    {
        _bookService = bookService;
        _fileService = fileService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var list = await _fileService.ReadListFromFileAsync<(string Id, int Count)>(maxCountFilePath);        
        _bookService.LoadCloneMaxCount(list);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var list = _bookService.ListCloneMaxCount();
        if (list.Count > 0)
        {
            await _fileService.WriteListToFileAsync(list, maxCountFilePath);
        }
    }
}
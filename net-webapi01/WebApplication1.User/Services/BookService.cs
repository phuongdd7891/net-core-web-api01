using Booklibrary;
using CoreLibrary.DbAccess;
using Grpc.Core;
using MongoDB.Driver;

namespace WebApplication1.User.Services;

public class BookService : BookLibraryServiceProto.BookLibraryServiceProtoBase
{
    private readonly MongoDbContext _mongoDbContext;
    private readonly IMongoCollection<CoreLibrary.DataModels.Book> _bookCollection;
    public BookService(
        MongoDbContext mongoDbContext
    )
    {
        _mongoDbContext = mongoDbContext;
        _bookCollection = mongoDbContext.GetCollection<CoreLibrary.DataModels.Book>("Books");
    }

    public override async Task<Common.CommonReply> CreateBook(CreateBookRequest request, ServerCallContext context)
    {
        var result = new Common.CommonReply();
        try
        {
            await _bookCollection.InsertOneAsync(new CoreLibrary.DataModels.Book
            {
                BookName = request.Title,
                Author = request.Author,
                Summary = request.Summary,
                Price = Convert.ToDecimal(request.Price),
                CoverPicture = request.CoverPicture,
                CreatedDate = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            result.Message = ex.Message;
        }
        return result;
    }

    public override async Task<Common.CommonReply> CreateBookWithUpload(IAsyncStreamReader<CreateBookUploadRequest> requestStream, ServerCallContext context)
    {
        var result = new Common.CommonReply();
        string? filePath = null;
        FileStream? fileStream = null;
        string fileName = string.Empty;

        var client = _mongoDbContext.GetClient();
        using var session = await client.StartSessionAsync();
        try
        {
            session.StartTransaction();
            await foreach (var request in requestStream.ReadAllAsync())
            {
                if (request.DataCase == CreateBookUploadRequest.DataOneofCase.BookData)
                {
                    await _bookCollection.InsertOneAsync(session, new CoreLibrary.DataModels.Book
                    {
                        BookName = request.BookData.Title,
                        Author = request.BookData.Author,
                        Summary = request.BookData.Summary,
                        Price = Convert.ToDecimal(request.BookData.Price),
                        CoverPicture = fileName,
                        CreatedDate = DateTime.UtcNow
                    });
                }
                else if (request.DataCase == CreateBookUploadRequest.DataOneofCase.Chunk)
                {
                    filePath = Path.Combine(request.Chunk.FileDir, request.Chunk.FileName);
                    fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                    if (fileStream != null)
                    {
                        await fileStream.WriteAsync(request.Chunk.ChunkData.ToByteArray());
                        fileName = request.Chunk.FileName;
                        fileStream.Close();
                    }
                }
            }
            await session.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            if (filePath != null)
            {
                File.Delete(filePath);
            }
            result.Message = $"Process failed: {ex.Message}";
        }
        finally
        {
            fileStream?.Close();
        }
        return result;
    }
}
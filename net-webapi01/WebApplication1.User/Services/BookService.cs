using Booklibrary;
using Common;
using CoreLibrary.DbAccess;
using Grpc.Core;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace WebApplication1.User.Services;

public class BookService : BookLibraryServiceProto.BookLibraryServiceProtoBase
{
    private readonly MongoDbContext _mongoDbContext;
    private readonly IMongoCollection<CoreLibrary.DataModels.Book> _bookCollection;
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(10);

    public BookService(
        MongoDbContext mongoDbContext
    )
    {
        _mongoDbContext = mongoDbContext;
        _bookCollection = mongoDbContext.GetCollection<CoreLibrary.DataModels.Book>("Books");
    }

    public override async Task<CommonReply> CreateBook(CreateBookRequest request, ServerCallContext context)
    {
        var result = new CommonReply();
        try
        {
            await _bookCollection.InsertOneAsync(new CoreLibrary.DataModels.Book
            {
                BookName = request.Title,
                Author = request.Author,
                Summary = request.Summary,
                Price = Convert.ToDecimal(request.Price),
                CoverPicture = request.CoverPicture,
                CreatedDate = DateTime.UtcNow,
                Category = request.Category
            });
        }
        catch (Exception ex)
        {
            result.Message = ex.Message;
        }
        return result;
    }

    public override async Task<CommonReply> CreateBookWithUpload(IAsyncStreamReader<CreateBookUploadRequest> requestStream, ServerCallContext context)
    {
        var result = new CommonReply();
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
                        CreatedDate = DateTime.UtcNow,
                        Category = request.BookData.Category
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

    public override async Task<CommonReply> UpdateBook(UpdateBookRequest request, ServerCallContext context)
    {
        var result = new CommonReply();
        try
        {
            var book = await _bookCollection.Find(a => a.Id == request.Id).FirstOrDefaultAsync();
            if (book == null)
            {
                result.Message = "Book not found";
            }
            else
            {
                await _bookCollection.ReplaceOneAsync(a => a.Id == request.Id, new CoreLibrary.DataModels.Book
                {
                    Id = request.Id,
                    BookName = request.Title ?? book.BookName,
                    Author = request.Author ?? book.Author,
                    Summary = request.Summary ?? book.Summary,
                    Price = request.Price != 0 ? Convert.ToDecimal(request.Price) : book.Price,
                    CoverPicture = book.CoverPicture,
                    CreatedDate = book.CreatedDate,
                    ModifiedDate = DateTime.UtcNow,
                    Category = request.Category ?? book.Category
                });
            }
        }
        catch (Exception ex)
        {
            result.Message = ex.Message;
        }
        return result;
    }

    public override async Task<CommonReply> UpdateBookWithUpload(IAsyncStreamReader<UpdateBookUploadRequest> requestStream, ServerCallContext context)
    {
        var result = new CommonReply();
        string? filePath = null;
        FileStream? fileStream = null;
        string? previousCoverPath = string.Empty;

        var client = _mongoDbContext.GetClient();
        using var session = await client.StartSessionAsync();
        try
        {
            session.StartTransaction();
            await foreach (var request in requestStream.ReadAllAsync())
            {
                if (request.DataCase == UpdateBookUploadRequest.DataOneofCase.BookData)
                {
                    var book = await _bookCollection.Find(session, a => a.Id == request.BookData.Id).FirstOrDefaultAsync();
                    if (book == null)
                    {
                        throw new Exception("Book not found");
                    }
                    await _bookCollection.ReplaceOneAsync(session, a => a.Id == request.BookData.Id, new CoreLibrary.DataModels.Book
                    {
                        Id = request.BookData.Id,
                        BookName = request.BookData.Title ?? book.BookName,
                        Author = request.BookData.Author ?? book.Author,
                        Summary = request.BookData.Summary ?? book.Summary,
                        Price = request.BookData.Price != 0 ? Convert.ToDecimal(request.BookData.Price) : book.Price,
                        CoverPicture = book.CoverPicture,
                        CreatedDate = book.CreatedDate,
                        ModifiedDate = DateTime.UtcNow,
                        Category = request.BookData.Category ?? book.Category
                    });
                }
                else if (request.DataCase == UpdateBookUploadRequest.DataOneofCase.Chunk)
                {
                    var book = await _bookCollection.Find(a => a.Id == request.Chunk.UpdateId).FirstOrDefaultAsync();
                    if (book == null)
                    {
                        throw new Exception("Book not found");
                    }
                    filePath = Path.Combine(request.Chunk.FileDir, request.Chunk.FileName);
                    fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                    if (fileStream != null)
                    {
                        await fileStream.WriteAsync(request.Chunk.ChunkData.ToByteArray());
                        var fileName = request.Chunk.FileName;
                        fileStream.Close();
                        previousCoverPath = !string.IsNullOrEmpty(book.CoverPicture) ? Path.Combine(request.Chunk.FileDir, book.CoverPicture) : string.Empty;
                        await _bookCollection.UpdateOneAsync(session, a => a.Id == request.Chunk.UpdateId, Builders<CoreLibrary.DataModels.Book>.Update.Set(a => a.CoverPicture, fileName));
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
            if (!string.IsNullOrEmpty(previousCoverPath))
            {
                File.Delete(previousCoverPath);
            }
        }
        return result;
    }

    public override async Task<GetBookReply> GetBook(GetBookRequest request, ServerCallContext context)
    {
        var reply = new GetBookReply();
        try
        {
            var book = await _bookCollection.Find(a => a.Id == request.Id).FirstOrDefaultAsync();
            if (book != null)
            {
                reply.Data = new Book
                {
                    Id = book.Id,
                    Title = book.BookName,
                    Author = book.Author,
                    Category = book.Category,
                    Price = Convert.ToDouble(book.Price),
                    Summary = book.Summary,
                    CoverPicture = book.CoverPicture
                };
            }
        }
        catch { }
        return reply;
    }

    private async Task InsertBatchAsync(List<CreateBookRequest> batch)
    {
        await _semaphore.WaitAsync();
        try
        {
            await _bookCollection.InsertManyAsync(batch.Select(book => new CoreLibrary.DataModels.Book
            {
                BookName = book.Title,
                Author = book.Author,
                Summary = book.Summary,
                Price = Convert.ToDecimal(book.Price),
                CoverPicture = book.CoverPicture,
                CreatedDate = DateTime.UtcNow,
                Category = book.Category,
                CloneId = book.CloneId
            }), new InsertManyOptions { IsOrdered = false });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override async Task<CommonReply> CreateBulkBooks(IAsyncStreamReader<CreateBulkRequest> requestStream, ServerCallContext context)
    {
        var reply = new CommonReply();
        try
        {
            int count = 0;
            long cloneCount = 1;
            while (await requestStream.MoveNext())
            {
                var batch = requestStream.Current.Data;
                if (batch != null && batch.Count > 0)
                {
                    if (count == 0)
                    {
                        var bId = batch[0].CloneId;
                        var filter = Builders<CoreLibrary.DataModels.Book>.Filter.Regex(a => a.CloneId, new MongoDB.Bson.BsonRegularExpression($"^{bId}#"));
                        var maxClone = _bookCollection.Find(filter).ToEnumerable().MaxBy(a => Convert.ToInt32(a.CloneId?.Replace($"{bId}#", "")));
                        if (maxClone != null)
                        {
                            cloneCount += Convert.ToInt32(maxClone.CloneId!.Replace($"{bId}#", ""));
                        }
                    }
                    await InsertBatchAsync(batch.Select(a =>
                    {
                        a.CloneId = $"{a.CloneId}#{cloneCount++}";
                        return a;
                    }).ToList());
                }
                count++;
            }
        }
        catch (Exception ex)
        {
            reply.Message = ex.Message;
        }
        return reply;
    }

    private async Task DeleteBatchAsync(List<string> ids)
    {
        await _semaphore.WaitAsync();
        try
        {
            var filter = Builders<CoreLibrary.DataModels.Book>.Filter.In(doc => doc.CloneId, ids);
            await _bookCollection.DeleteManyAsync(filter);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override async Task<CommonReply> DeleteBulkBooks(DeleteBulkRequest request, ServerCallContext context)
    {
        var reply = new CommonReply();
        try
        {
            var cloneIds = new List<string>();
            for (int i = request.FromOrder; i <= request.ToOrder; i++)
            {
                cloneIds.Add($"{request.Id}#{i}");
            }
            const int batchSize = 1000;
            var tasks = new List<Task>();

            for (int i = 0; i < cloneIds.Count; i += batchSize)
            {
                var batch = cloneIds.Skip(i).Take(batchSize).ToList();
                tasks.Add(DeleteBatchAsync(batch));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            reply.Message = ex.Message;
        }
        return reply;
    }
}
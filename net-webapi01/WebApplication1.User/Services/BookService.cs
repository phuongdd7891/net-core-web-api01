using Booklibrary;
using Common;
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
        return reply;
    }
}
using Microsoft.AspNetCore.Mvc;
using Fileuploadservice;
using Google.Protobuf;
using CoreLibrary.Repository;
using Booklibrary;
using Gateway.Models;
using Microsoft.Extensions.Options;
using Common;

namespace Gateway.Controllers;

[ApiController]
[Route("gw-api/[controller]")]
public class BookController : BaseController
{
    private readonly UploadSettings _uploadSettings;
    private readonly RedisRepository _redisRepository;
    private readonly UploadServiceProto.UploadServiceProtoClient _uploadServiceClient;
    private readonly BookLibraryServiceProto.BookLibraryServiceProtoClient _bookServiceClient;
    private readonly string[] allowedMimeTypes = { "image/jpeg", "image/png", "image/jpg" };

    public BookController(
        IOptions<UploadSettings> uploadSettings,
        RedisRepository redisRepository,
        UploadServiceProto.UploadServiceProtoClient uploadServiceClient,
        BookLibraryServiceProto.BookLibraryServiceProtoClient bookServiceClient
    )
    {
        _uploadSettings = uploadSettings.Value;
        _redisRepository = redisRepository;
        _uploadServiceClient = uploadServiceClient;
        _bookServiceClient = bookServiceClient;
    }

    private async Task<string> GetUniqueFileName(string fileName)
    {
        string uniqueFileName = fileName;
        int counter = 1;

        while (await _redisRepository.SetContains(_uploadSettings.CacheName, uniqueFileName))
        {
            uniqueFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{counter}{Path.GetExtension(fileName)}";
            counter++;
        }
        await _redisRepository.SetAdd(_uploadSettings.CacheName, uniqueFileName);
        return uniqueFileName;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile formfile, string? fileName = null)
    {
        ErrorStatuses.ThrowBadRequest("File is required", formfile == null || formfile.Length == 0);
        ErrorStatuses.ThrowBadRequest("Invalid file type", !allowedMimeTypes.Contains(formfile?.ContentType));

        using var fileStream = formfile!.OpenReadStream();
        var call = _uploadServiceClient.UploadFile();
        var uniqueFileName = await GetUniqueFileName(fileName ?? formfile.FileName);

        await call.RequestStream.WriteAsync(new FileChunk
        {
            FileName = uniqueFileName,
            FileDir = _uploadSettings.UploadDir
        });

        byte[] buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await call.RequestStream.WriteAsync(new FileChunk
            {
                ChunkData = ByteString.CopyFrom(buffer, 0, bytesRead)
            });
        }

        await call.RequestStream.CompleteAsync();
        var response = await call.ResponseAsync;

        ErrorStatuses.ThrowInternalErr(response.Message, !string.IsNullOrEmpty(response.Message));
        return Ok(new DataResponse
        {
            Data = "File uploaded successfully"
        });
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadFile(string name)
    {
        ErrorStatuses.ThrowInternalErr("Invalid request", string.IsNullOrEmpty(name));
        var path = Path.Combine(_uploadSettings.UploadDir, name);
        var result = await _uploadServiceClient.DownloadFileAsync(new DownloadRequest
        {
            FileName = path
        });
        if (result.Data != ByteString.Empty)
        {
            return File(result.Data.ToByteArray(), $"image/{Path.GetExtension(path).Split(".")[1]}");
        }
        return NotFound(new DataResponse
        {
            Data = result.Message
        });
    }

    [HttpGet("download-cover")]
    public async Task<IActionResult> DownloadCover(string id)
    {
        ErrorStatuses.ThrowInternalErr("Invalid request", string.IsNullOrEmpty(id));
        var book = await _bookServiceClient.GetBookAsync(new GetBookRequest
        {
            Id = id
        });
        ErrorStatuses.ThrowNotFound("Book not found", book.Data == null);
        return await DownloadFile(book.Data!.CoverPicture);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(Models.Requests.CreateBookRequest request)
    {
        ErrorStatuses.ThrowInternalErr("Invalid request", request.Data == null);
        var bookRequest = new CreateBookRequest
        {
            Title = request.Data!.BookName,
            Author = request.Data.Author,
            Summary = request.Data.Summary,
            Price = Convert.ToDouble(request.Data.Price)
        };
        if (request.FileData != null)
        {
            ErrorStatuses.ThrowBadRequest("Invalid file type", !allowedMimeTypes.Contains(request.FileData.ContentType));
            using var call = _bookServiceClient.CreateBookWithUpload();
            var uniqueFileName = await GetUniqueFileName(request.FileData.FileName);

            byte[] buffer = new byte[4096];
            int bytesRead;
            using var fileStream = request.FileData.OpenReadStream();
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await call.RequestStream.WriteAsync(new CreateBookUploadRequest
                {
                    Chunk = new FileChunk
                    {
                        FileName = uniqueFileName,
                        FileDir = _uploadSettings.UploadDir,
                        ChunkData = ByteString.CopyFrom(buffer, 0, bytesRead)
                    }
                });
            }

            await call.RequestStream.WriteAsync(new CreateBookUploadRequest
            {
                BookData = bookRequest
            });

            await call.RequestStream.CompleteAsync();
            var response = await call.ResponseAsync;
            if (!string.IsNullOrEmpty(response.Message))
            {
                await _redisRepository.SetRemove(_uploadSettings.CacheName, uniqueFileName);
            }
            ErrorStatuses.ThrowInternalErr(response.Message, !string.IsNullOrEmpty(response.Message));
        }
        else
        {
            var createReply = await _bookServiceClient.CreateBookAsync(bookRequest);
            ErrorStatuses.ThrowInternalErr(createReply.Message, !string.IsNullOrEmpty(createReply.Message));
        }
        return Ok(new DataResponse
        {
            Data = "Created successfully"
        });
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update(Models.Requests.UpdateBookRequest request)
    {
        ErrorStatuses.ThrowInternalErr("Invalid request", string.IsNullOrEmpty(request.Id));
        if (request.FileData != null)
        {
            ErrorStatuses.ThrowBadRequest("Invalid file type", !allowedMimeTypes.Contains(request.FileData.ContentType));
            using var call = _bookServiceClient.UpdateBookWithUpload();
            var uniqueFileName = await GetUniqueFileName(request.FileData.FileName);

            byte[] buffer = new byte[4096];
            int bytesRead;
            using var fileStream = request.FileData.OpenReadStream();
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await call.RequestStream.WriteAsync(new UpdateBookUploadRequest
                {
                    Chunk = new FileChunk
                    {
                        FileName = uniqueFileName,
                        FileDir = _uploadSettings.UploadDir,
                        UpdateId = request.Id,
                        ChunkData = ByteString.CopyFrom(buffer, 0, bytesRead)
                    }
                });
            }
            if (request.Data != null)
            {
                var bookRequest = new UpdateBookRequest
                {
                    Id = request.Id,
                    Title = request.Data.BookName,
                    Author = request.Data.Author,
                    Summary = request.Data.Summary,
                    Price = Convert.ToDouble(request.Data.Price),
                    Category = request.Data.Category
                };
                await call.RequestStream.WriteAsync(new UpdateBookUploadRequest
                {
                    BookData = bookRequest
                });
            }

            await call.RequestStream.CompleteAsync();
            var response = await call.ResponseAsync;
            if (!string.IsNullOrEmpty(response.Message))
            {
                await _redisRepository.SetRemove(_uploadSettings.CacheName, uniqueFileName);
            }
            ErrorStatuses.ThrowInternalErr(response.Message, !string.IsNullOrEmpty(response.Message));
        }
        else
        {
            ErrorStatuses.ThrowInternalErr("Invalid request", request.Data == null);
            var bookRequest = new UpdateBookRequest
            {
                Id = request.Id,
                Title = request.Data!.BookName,
                Author = request.Data.Author,
                Summary = request.Data.Summary,
                Price = Convert.ToDouble(request.Data.Price),
                Category = request.Data.Category
            };
            var updateReply = await _bookServiceClient.UpdateBookAsync(bookRequest);
            ErrorStatuses.ThrowInternalErr(updateReply.Message, !string.IsNullOrEmpty(updateReply.Message));
        }
        return Ok(new DataResponse
        {
            Data = "Updated successfully"
        });
    }

    [HttpPost("create-clone")]
    public async Task<IActionResult> CreateClone(Models.Requests.CloneBookRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(request.Id) || request.Quantity == 0);
        var book = await _bookServiceClient.GetBookAsync(new GetBookRequest
        {
            Id = request.Id
        });
        ErrorStatuses.ThrowNotFound("Book not found", book.Data == null);
        int count = 0;
        var list = new List<CreateBookRequest>();
        while (count < request.Quantity)
        {
            list.Add(new CreateBookRequest
            {
                Title = $"[clone] {book.Data!.Title}",
                Author = book.Data.Author,
                Summary = book.Data.Summary,
                Price = Convert.ToDouble(book.Data.Price),
                Category = book.Data.Category,
                CoverPicture = book.Data.CoverPicture,
                CloneId = request.Id
            });
            count++;
        }

        using var call = _bookServiceClient.CreateBulkBooks();
        const int batchSize = 1000;
        for (int i = 0; i < list.Count; i += batchSize)
        {
            var batch = new CreateBulkRequest();
            batch.Data.AddRange(list.GetRange(i, Math.Min(batchSize, list.Count - i)));
            await call.RequestStream.WriteAsync(batch);
        }

        await call.RequestStream.CompleteAsync();

        var response = await call;
        ErrorStatuses.ThrowInternalErr(response.Message, !string.IsNullOrEmpty(response.Message));
        return Ok(new DataResponse
        {
            Data = "Cloned successfully"
        });
    }

    [HttpPost("delete-clone")]
    public async Task<IActionResult> DeleteClone(Models.Requests.DeleteCloneBooksRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(request.Id) || request.From > request.To);
        var response = await _bookServiceClient.DeleteBulkBooksAsync(new DeleteBulkRequest
        {
            Id = request.Id,
            FromOrder = request.From,
            ToOrder = request.To
        });
        ErrorStatuses.ThrowInternalErr(response.Message, !string.IsNullOrEmpty(response.Message));
        return Ok(new DataResponse
        {
            Data = "Deleted successfully"
        });

    }
}
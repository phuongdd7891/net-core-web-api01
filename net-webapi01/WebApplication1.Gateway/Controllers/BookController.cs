using Microsoft.AspNetCore.Mvc;
using Fileuploadservice;
using Google.Protobuf;
using CoreLibrary.Repository;
using Booklibrary;
using Gateway.Models;
using Microsoft.Extensions.Options;
using Gateway.Models.Response;
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

    [HttpPost("create")]
    public async Task<IActionResult> Create(Models.Requests.CreateBookRequest request)
    {
        var bookRequest = new CreateBookRequest
        {
            Title = request.Data.BookName,
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
}
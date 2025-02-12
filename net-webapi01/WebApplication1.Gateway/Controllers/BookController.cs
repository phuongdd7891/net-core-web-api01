using Microsoft.AspNetCore.Mvc;
using Fileuploadservice;
using Google.Protobuf;
using Microsoft.AspNetCore.Http.HttpResults;
using CoreLibrary.Repository;
using Gateway.Services;
using Gateway.Models;
using Microsoft.Extensions.Options;

namespace Gateway.Controllers;

[ApiController]
[Route("gw-api/[controller]")]
public class BookController : BaseController
{
    private readonly UploadSettings _uploadSettings;
    private readonly RedisRepository _redisRepository;
    private readonly UploadServiceProto.UploadServiceProtoClient _uploadServiceClient;
    private readonly string[] allowedMimeTypes = { "image/jpeg", "image/png", "image/jpg" };

    public BookController(
        IOptions<UploadSettings> uploadSettings,
        RedisRepository redisRepository,
        UploadServiceProto.UploadServiceProtoClient uploadServiceClient
    )
    {
        _uploadSettings = uploadSettings.Value;
        _redisRepository = redisRepository;
        _uploadServiceClient = uploadServiceClient;
    }

    private async Task<string> GetUniqueFileName(string fileName)
    {
        string uniqueFileName = fileName;
        const int maxRetries = 10;
        int attempt = 0;

        while (attempt < maxRetries)
        {
            if (!await _redisRepository.SetContains(_uploadSettings.CacheName, uniqueFileName))
            {
                await _redisRepository.SetAdd(_uploadSettings.CacheName, uniqueFileName);
                return uniqueFileName;
            }
            string fileExtension = Path.GetExtension(fileName);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            uniqueFileName = $"{nameWithoutExtension}_{Guid.NewGuid().ToString("N")}{fileExtension}";
            attempt++;
        }
        await _redisRepository.SetAdd(_uploadSettings.CacheName, uniqueFileName);
        return uniqueFileName;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile formfile, string? fileName)
    {
        ErrorStatuses.ThrowBadRequest("File is required", formfile == null || formfile.Length == 0);
        ErrorStatuses.ThrowBadRequest("Invalid file type", !allowedMimeTypes.Contains(formfile?.ContentType));
        using var fileStream = formfile!.OpenReadStream();
        var call = _uploadServiceClient.UploadFile();
        var name = string.IsNullOrEmpty(fileName) ? formfile.FileName : fileName;
        var uniqueFileName = await GetUniqueFileName(name);

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
}
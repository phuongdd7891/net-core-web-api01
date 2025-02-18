using Fileuploadservice;
using Grpc.Core;
using Common;
using CoreLibrary.Utils;
using Newtonsoft.Json;

namespace WebApplication1.User.Services;

public class FileService : UploadServiceProto.UploadServiceProtoBase
{
    public override async Task<UploadReply> UploadFile(IAsyncStreamReader<FileChunk> requestStream, ServerCallContext context)
    {
        string? filePath = null;
        FileStream? fileStream = null;
        var reply = new UploadReply();
        try
        {
            await foreach (var chunk in requestStream.ReadAllAsync())
            {
                if (string.IsNullOrEmpty(chunk.FileDir) && chunk.ChunkData == null)
                {
                    reply.Message = "Directory not found";
                    break;
                }
                if (filePath == null)
                {
                    filePath = Path.Combine(chunk.FileDir, chunk.FileName);
                    fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                }
                if (fileStream != null)
                {
                    await fileStream.WriteAsync(chunk.ChunkData.ToByteArray(), 0, chunk.ChunkData.Length);
                }
            }
            return reply;
        }
        catch (Exception ex)
        {
            return new UploadReply { Message = $"File upload failed: {ex.Message}" };
        }
        finally
        {
            fileStream?.Close();
        }
    }

    public override Task<DownloadReply> DownloadFile(DownloadRequest request, ServerCallContext context)
    {
        var reply = new DownloadReply();
        try
        {
            var bytes = Utils.ConvertToByteArrayChunked(request.FileName);
            if (bytes?.Length > 0)
            {
                reply.Data = Google.Protobuf.ByteString.CopyFrom(bytes);
            }
        }
        catch (FileNotFoundException ex)
        {
            reply.Message = ex.Message;
        }

        return Task.FromResult(reply);
    }

    public async Task WriteListToFileAsync<T>(List<T> list, string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var item in list)
            {
                var obj = JsonConvert.SerializeObject(item);
                await writer.WriteLineAsync(obj);
            }
        }
    }

    public async Task<List<T>> ReadListFromFileAsync<T>(string filePath)
    {
        var list = new List<T>();
        if (File.Exists(filePath))
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string? line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                {
                    var item = JsonConvert.DeserializeObject<T>(line);
                    list.Add(item!);
                }
            }
        }

        return list;
    }
}
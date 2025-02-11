using Fileuploadservice;
using Grpc.Core;
using Common;

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
}
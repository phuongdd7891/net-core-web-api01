namespace CoreLibrary.Utils;

public class Utils
{
    public static byte[] ConvertToByteArrayChunked(string filePath)
    {
        const int MaxChunkSizeInBytes = 2048;
        var totalBytes = 0;
        byte[] fileByteArray;
        var fileByteArrayChunk = new byte[MaxChunkSizeInBytes];

        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            int bytesRead;

            while ((bytesRead = stream.Read(fileByteArrayChunk, 0, fileByteArrayChunk.Length)) > 0)
            {
                totalBytes += bytesRead;
            }

            fileByteArray = new byte[totalBytes];
            stream.Position = 0;
            stream.Read(fileByteArray, 0, totalBytes);
        }

        return fileByteArray;
    }
}
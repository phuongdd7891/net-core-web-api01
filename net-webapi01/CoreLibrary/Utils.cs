using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using QRCoder;

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

    public static bool ValidEmailAddress(string emailAddress)
    {
        var pattern = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$";
        var regex = new Regex(pattern);
        return regex.IsMatch(emailAddress);
    }

    public static bool ValidPhoneNumber(string phoneNumber)
    {
        var phoneValidation = new PhoneAttribute();
        return phoneValidation.IsValid(phoneNumber);
    }

    public static string GenerateQRFromText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }
        QRCodeGenerator qrGenerator = new QRCodeGenerator();
        QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        Base64QRCode qrCode = new Base64QRCode(qrCodeData);
        var qrCodeBase64 = qrCode.GetGraphic(20);
        return qrCodeBase64;
    }
}
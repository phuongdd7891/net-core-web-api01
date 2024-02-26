using CoreLibrary.Helpers;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using NLog;
using NLog.Web;
using WebApi.Models;

namespace WebApi.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
}

public class EmailService : IEmailSender
{
    private readonly EmailSettings _emailSettings;
    private readonly Logger _logger = LogManager.Setup()
                       .LoadConfigurationFromAppSettings()
                       .GetCurrentClassLogger();

    public EmailService(
        IOptions<EmailSettings> emailSettings
    )
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        try
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.Sender));
            mimeMessage.To.Add(new MailboxAddress(email, email));
            mimeMessage.Subject = subject;
            mimeMessage.Body = new TextPart("html")
            {
                Text = message
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, true);
                var sender = AESHelpers.Decrypt(_emailSettings.Sender!);
                var password = AESHelpers.Decrypt(_emailSettings.Password!);
                await client.AuthenticateAsync(sender, password);
                await client.SendAsync(mimeMessage);
                await client.DisconnectAsync(true);
            }

        }
        catch (Exception ex)
        {
            _logger.Error(ex, message);
            throw new InvalidOperationException(ex.Message);
        }
    }
}
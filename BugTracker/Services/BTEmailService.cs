using BugTracker.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
namespace BugTracker.Services;

public class BTEmailService : IEmailSender
{
    //private readonly MailSettings _mailSettings;
    private readonly IConfiguration _config;

    public BTEmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string emailTo, string subject, string htmlMessage)
    {
        MimeMessage email = new();
        email.Sender = MailboxAddress.Parse(_config["Email:Mail"]);

        var builder = new BodyBuilder
        {
            HtmlBody = htmlMessage
        };

        email.Body = builder.ToMessageBody();

        try
        {
            using var smtp = new SmtpClient();
            //smtp.Connect(_mailSettings.Host, _mailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            smtp.Connect(_config["MailSettings:Host"], Int32.Parse(_config["MailSettings:Port"]), MailKit.Security.SecureSocketOptions.StartTls);
            smtp.Authenticate(_config["Email:Mail"], _config["Email:Password"]);

            await smtp.SendAsync(email);

            smtp.Disconnect(true);
        }
        catch (Exception)
        {

            throw;
        }
    }
}

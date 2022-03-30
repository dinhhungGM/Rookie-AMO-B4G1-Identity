using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace Rookie.AMO.Identity.Bussiness.Services
{
    public class EmailSenderService : IEmailSender
    {
        private readonly IConfiguration _config;
        public EmailSenderService(IConfiguration config)
        {
            _config = config;
        }
        public Task SendEmailAsync(string email, string subject, string message)
        {
            var apiKey = _config.GetSection("ExternalProviders").GetSection("SendGrid").GetSection("ApiKey").Value;
            return Execute(apiKey, subject, message, email);
        }

        public Task Execute(string apiKey, string subject, string message, string email)
        {
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(_config.GetSection("ExternalProviders").GetSection("SendGrid").GetSection("SenderEmail").Value, "Admin"),
                Subject = subject,
                PlainTextContent = message,

                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(email));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);

            var res = client.SendEmailAsync(msg);

            return res;
        }


    }
}

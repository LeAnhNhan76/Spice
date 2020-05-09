using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using Spice.Common;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace Spice.Services
{
    public class EmailSender : IEmailSender
    {
        public EmailOptions options { get; set; }
        public EmailSender(IOptions<EmailOptions> emailOptions)
        {
            options = emailOptions.Value; 
        }
        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Execute(options.SendGridKey, subject, message, email);
        }

        private Task Execute(string sendGridKey, string subject, string message, string email)
        {
            var client = new SendGridClient(sendGridKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(Constant.Email_From, Constant.Email_CompanyName),
                Subject = subject, 
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(email));
            try
            {
                return client.SendEmailAsync(msg);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }
    }
}
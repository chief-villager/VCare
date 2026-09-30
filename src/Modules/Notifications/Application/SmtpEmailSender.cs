using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Notifications.Application.Contract;
using Notifications.Domain.Entity;

namespace Notifications.Application
{
    internal class SmtpEmailSender(IOptions<SmtpOptions> options) : ISmtpEmailSender
    {
        private readonly SmtpOptions _options = options.Value;

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_options.Host, _options.Port) { EnableSsl = _options.UseSsl };
            if (!string.IsNullOrEmpty(_options.User))
                client.Credentials = new NetworkCredential(_options.User, _options.Password);

            await client.SendMailAsync(message, ct);
   }   }   

}
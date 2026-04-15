using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration.Sections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly ConfigurationStructure _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
        IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            this._configuration = configuration.Get<ConfigurationStructure>()!;
            _logger = logger;
        }

        public async Task SendAsync(
            string to, string subject, string htmlBody, string? cc = null, string? bcc = null,
            CancellationToken cancellationToken = default
        )
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_configuration.SMTP.From.Address, _configuration.SMTP.From.Name),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(to);

            if (!string.IsNullOrWhiteSpace(cc))
                message.CC.Add(cc);

            if (!string.IsNullOrWhiteSpace(bcc))
                message.Bcc.Add(bcc);

            using var client = new SmtpClient(_configuration.SMTP.Connection.Host, _configuration.SMTP.Connection.Port)
            {
                Credentials = new NetworkCredential(
                    _configuration.SMTP.Authentication.Username,
                    _configuration.SMTP.Authentication.Password
                ),
                EnableSsl = _configuration.SMTP.Connection.UseTls
            };

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent to {To} with subject '{Subject}'", to, subject);
        }
    }
}

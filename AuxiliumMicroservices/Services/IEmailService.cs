using System;
using System.Collections.Generic;
using System.Text;

namespace AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody,
            string? cc = null, string? bcc = null,
            CancellationToken cancellationToken = default);
    }
}

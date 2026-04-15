using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.Services.Implementations
{
    public class EmailTemplateRenderer : IEmailTemplateRenderer
    {
        private readonly ILogger<EmailTemplateRenderer> _logger;

        private static readonly Dictionary<string, string> Templates = new()
        {
            ["force-password-reset"] = """
                <h2>Password Change Required</h2>
                <p>Hi {{displayName}},</p>
                <p>An administrator has required you to change your password.
                You will be prompted to set a new password on your next login.</p>
                <p>If you did not expect this, please contact your administrator.</p>
                """,

            ["password-reset"] = """
                <h2>Password Reset</h2>
                <p>Hi {{displayName}},</p>
                <p>A password reset has been requested for your account.
                Click the link below to set a new password:</p>
                <p><a href="{{resetLink}}">Reset Password</a></p>
                <p>This link will expire in {{expiryHours}} hours.</p>
                <p>If you did not request this, you can safely ignore this email.</p>
                """,

            ["fallback"] = """
                <p>{{body}}</p>
                """
        };

        public EmailTemplateRenderer(ILogger<EmailTemplateRenderer> logger)
        {
            _logger = logger;
        }

        public string Render(string templateName, Dictionary<string, string> data)
        {
            if (!Templates.TryGetValue(templateName, out var template))
            {
                _logger.LogWarning("Template '{TemplateName}' not found, using fallback", templateName);
                template = Templates["fallback"];
            }

            foreach (var (key, value) in data)
            {
                template = template.Replace($"{{{{{key}}}}}", value);
            }

            return template;
        }
    }
}

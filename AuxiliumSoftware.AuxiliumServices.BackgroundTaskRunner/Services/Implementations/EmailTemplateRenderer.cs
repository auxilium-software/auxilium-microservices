using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.Services.Implementations
{
    public class EmailTemplateRenderer : IEmailTemplateRenderer
    {
        private readonly ILogger<EmailTemplateRenderer> _logger;
        private readonly Assembly _assembly;
        private readonly string _assemblyName;
        private readonly Dictionary<string, Dictionary<string, string>> _translations;

        public EmailTemplateRenderer(
            ILogger<EmailTemplateRenderer> logger
        )
        {
            _logger = logger;
            _assembly = Assembly.GetExecutingAssembly();
            _assemblyName = _assembly.GetName().Name!;
            _translations = LoadTranslations();
        }

        public string Render(string templateName, string locale, Dictionary<string, string> data)
        {
            var resourceName = $"{_assemblyName}.Templates.Emails.{templateName}.html";
            var templateText = ReadResource(resourceName);

            if (templateText == null)
            {
                _logger.LogWarning("Template '{Name}' not found as embedded resource '{Resource}'",
                    templateName, resourceName);
                return $"<p>{WebUtility.HtmlEncode(data.GetValueOrDefault("body", ""))}</p>";
            }

            var template = Template.Parse(templateText);

            if (template.HasErrors)
            {
                _logger.LogError("Template parse errors in '{Name}': {Errors}",
                    templateName, string.Join("; ", template.Messages));
                throw new InvalidOperationException($"Template '{templateName}' has parse errors");
            }

            var scriptObject = new ScriptObject();

            // register t() - looks up the locale, falls back to the key itself (which is English)
            scriptObject.Import("t", new Func<string, string>(key => Translate(key, locale)));

            // add template variables (html-encoded)
            foreach (var (key, value) in data)
            {
                scriptObject.Add(key, WebUtility.HtmlEncode(value));
            }

            // expose locale for the <html lang=""> attribute
            scriptObject.Add("locale", locale);

            var context = new TemplateContext();
            context.PushGlobal(scriptObject);

            return template.Render(context);
        }

        public string TranslateSubject(string subject, string locale)
        {
            return Translate(subject, locale);
        }

        private string Translate(string key, string locale)
        {
            if (_translations.TryGetValue(key, out var locales)
                && locales.TryGetValue(locale, out var translated)
                && !string.IsNullOrEmpty(translated))
            {
                return translated;
            }

            return key;
        }

        private Dictionary<string, Dictionary<string, string>> LoadTranslations()
        {
            var resourceName = $"{_assemblyName}.Translations.json";
            var json = ReadResource(resourceName);

            if (json == null)
            {
                _logger.LogError("Translations resource '{Resource}' not found", resourceName);
                return new Dictionary<string, Dictionary<string, string>>();
            }

            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json)
                ?? new Dictionary<string, Dictionary<string, string>>();
        }

        private string? ReadResource(string resourceName)
        {
            using var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return null;

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}

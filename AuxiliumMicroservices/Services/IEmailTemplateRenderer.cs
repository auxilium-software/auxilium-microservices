using System.Collections.Generic;

namespace AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.Services
{
    public interface IEmailTemplateRenderer
    {
        string Render(string templateName, string locale, Dictionary<string, string> data);
        string TranslateSubject(string subject, string locale);
    }
}

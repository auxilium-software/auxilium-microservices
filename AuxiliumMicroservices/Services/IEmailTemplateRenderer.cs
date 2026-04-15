using System;
using System.Collections.Generic;
using System.Text;

namespace AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.Services
{
    public interface IEmailTemplateRenderer
    {
        string Render(string templateName, string locale, Dictionary<string, string> data);
    }
}

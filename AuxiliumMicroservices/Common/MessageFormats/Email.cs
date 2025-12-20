using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AuxiliumMicroservices.Common.MessageFormats
{
    internal class Email : DefinedObject
    {
        internal Person? Sender { get; }
        internal Person? ToRecipient { get; }
        internal string? About { get; }
        internal string? Text { get; }
        internal string? Encoding { get; }
        internal string? EncodingFormat { get; }
        internal string? ArticleBody { get; }

        internal Email(Dictionary<string, object> message) : base(message, "EmailMessage")
        {
            this.Sender = new Person((Dictionary<string, object>)message["sender"]);

            this.ToRecipient = new Person((Dictionary<string, object>)message["toRecipient"]);

            About = message.TryGetValue("about", out var aboutValue) && aboutValue is string about
                ? about
                : null;

            Text = message.TryGetValue("text", out var textValue) && textValue is string text
                ? text
                : null;

            Encoding = message.TryGetValue("encoding", out var encodingValue) && encodingValue is string encoding
                ? encoding
                : null;

            EncodingFormat = message.TryGetValue("encodingFormat", out var encodingFormatValue) && encodingFormatValue is string encodingFormat
                ? encodingFormat
                : null;

            ArticleBody = message.TryGetValue("articleBody", out var articleBodyValue) && articleBodyValue is string articleBody
                ? articleBody
                : null;
        }
    }
}

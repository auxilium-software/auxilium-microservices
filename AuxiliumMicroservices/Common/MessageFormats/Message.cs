using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuxiliumMicroservices.Common.MessageFormats
{
    internal class Message
    {
        internal string? Context { get; set; }
        internal string? Type { get; set; }

        internal Message(Dictionary<string, object> message)
        {
            Context = message.TryGetValue("@context", out var contextValue) && contextValue is string ctx
                ? ctx
                : null;

            Type = message.TryGetValue("@type", out var typeValue) && typeValue is string type
                ? type
                : null;
        }
    }
}

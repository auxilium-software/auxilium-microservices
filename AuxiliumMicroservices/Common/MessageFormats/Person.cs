using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuxiliumMicroservices.Common.MessageFormats
{
    internal class Person : DefinedObject
    {
        internal string? Name { get; }
        internal string? Email { get; }

        internal Person(Dictionary<string, object> message) : base(message, "Person")
        {
            Name = message.TryGetValue("name", out var nameValue) && nameValue is string name
                ? name
                : null;

            Email = message.TryGetValue("email", out var emailValue) && emailValue is string email
                ? email
                : null;
        }
    }
}

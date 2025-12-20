using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace AuxiliumMicroservices.Common.MessageFormats
{
    internal class DefinedObject
    {
        internal string? Type { get; set; }

        internal DefinedObject(Dictionary<string, object> message, string type)
        {
            Type = message.TryGetValue("@type", out var typeValue) && typeValue is string _type
                ? _type
                : null;

            if (!(this.Type?.Equals(type) ?? false))
            {
                throw new ArgumentException(message: "Dynamic object is not of the correct type");
            }
        }

        internal static Dictionary<string, object> ParseJsonToDictionary(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            using JsonDocument doc = JsonDocument.Parse(json);
            return JsonElementToDictionary(doc.RootElement);
        }

        internal static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
        {
            var dictionary = new Dictionary<string, object>();

            foreach (JsonProperty property in element.EnumerateObject())
            {
                dictionary[property.Name] = GetValue(property.Value);
            }

            return dictionary;
        }

        internal static object GetValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Object => JsonElementToDictionary(element),
                JsonValueKind.Array => element.EnumerateArray().Select(GetValue).ToArray(),
                JsonValueKind.Null => null,
                _ => element.ToString()
            };
        }
    }
}

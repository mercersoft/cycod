using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace Cycodlib;

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(List<ChatMessage>))]
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(object))]
public partial class CycodlibJsonContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<ChatMessage>))]
[JsonSerializable(typeof(ChatMessage))]
public partial class CycodlibIndentedJsonContext : JsonSerializerContext
{
}
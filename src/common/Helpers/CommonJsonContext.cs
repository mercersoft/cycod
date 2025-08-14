using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Helpers;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<Dictionary<string, string>>))]
[JsonSerializable(typeof(Dictionary<string, List<string>>))]
public partial class CommonJsonContext : JsonSerializerContext
{
}
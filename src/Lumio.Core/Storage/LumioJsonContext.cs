using System.Text.Json.Serialization;
using Lumio.Core.Model;

namespace Lumio.Core.Storage;

/// <summary>Source-generated JSON metadata, so persistence stays trimming and AOT safe.</summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ConnectionsDocument))]
internal sealed partial class LumioJsonContext : JsonSerializerContext;

/// <summary>The on-disk shape of <c>connections.json</c>. Versioned for future migrations.</summary>
internal sealed record ConnectionsDocument
{
    public int Version { get; init; } = 1;
    public List<Connection> Connections { get; init; } = [];
}

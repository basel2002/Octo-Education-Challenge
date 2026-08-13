using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProgramDesigner.Core.Domain;

/// <summary>
/// A domain value object representing the unique identifier for any ProgramNode.
/// </summary>
[JsonConverter(typeof(NodeIdJsonConverter))]
public readonly record struct NodeId(Guid Value)
{
    public static NodeId NewNodeId() => new(Guid.NewGuid());
    
    public override string ToString() => Value.ToString();

    public static implicit operator Guid(NodeId nodeId) => nodeId.Value;
    public static implicit operator NodeId(Guid guid) => new NodeId(guid);
}

public class NodeIdJsonConverter : JsonConverter<NodeId>
{
    public override NodeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && Guid.TryParse(reader.GetString(), out var guid))
        {
            return new NodeId(guid);
        }

        throw new JsonException("Expected a valid GUID string for NodeId.");
    }

    public override void Write(Utf8JsonWriter writer, NodeId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

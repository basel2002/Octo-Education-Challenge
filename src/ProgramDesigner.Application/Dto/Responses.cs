namespace ProgramDesigner.Application.Dto;

using System.Text.Json.Serialization;
using ProgramDesigner.Core.Domain;

public sealed record ProgramResponse
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required GroupNodeResponse RootGroup { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StepNodeResponse), typeDiscriminator: "step")]
[JsonDerivedType(typeof(GroupNodeResponse), typeDiscriminator: "group")]
public abstract record ProgramNodeResponse
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public Guid? PrerequisiteId { get; init; }
    public string? PrerequisiteName { get; init; }
}

public sealed record StepNodeResponse : ProgramNodeResponse
{
    public required string StepType { get; init; }
}

public sealed record GroupNodeResponse : ProgramNodeResponse
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required GroupRule GroupRule { get; init; }
    
    public int? PickCount { get; init; }
    
    public List<ProgramNodeResponse> Children { get; init; } = [];
}

namespace ProgramDesigner.Application.Dto;

using System.Text.Json.Serialization;
using ProgramDesigner.Core.Domain;

public sealed record ProgramCreateRequest
{
    public required string Name { get; init; }
    public required GroupNodeRequest RootGroup { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StepNodeRequest), typeDiscriminator: "step")]
[JsonDerivedType(typeof(GroupNodeRequest), typeDiscriminator: "group")]
public abstract record ProgramNodeRequest
{
    public string? Key { get; init; }
    public required string Name { get; init; }
    public string? PrerequisiteRef { get; init; }
}

public sealed record StepNodeRequest : ProgramNodeRequest
{
    public required string StepType { get; init; }
}

public sealed record GroupNodeRequest : ProgramNodeRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required GroupRule GroupRule { get; init; }
    
    public int? PickCount { get; init; }
    
    public List<ProgramNodeRequest> Children { get; init; } = [];
}

public sealed record ProgramSimulationRequest
{
    public Dictionary<Guid, List<Guid>> Choices { get; init; } = [];
    public List<Guid> CompletedStepIds { get; init; } = [];
}

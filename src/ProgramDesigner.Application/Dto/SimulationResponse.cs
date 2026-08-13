namespace ProgramDesigner.Application.Dto;

using System.Text.Json.Serialization;
using ProgramDesigner.Core.Domain;

public sealed record ProgramSimulationResponse
{
    public required ProgramSimulationNodeResponse RootNode { get; init; }
}

public sealed record ProgramSimulationNodeResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required NodeType NodeType { get; init; }
    public required SimulationStatus Status { get; init; }
    public string? BlockedReason { get; init; }
    public List<ProgramSimulationNodeResponse> Children { get; init; } = [];
}

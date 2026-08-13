namespace ProgramDesigner.Core.Domain;

public sealed record ProgramSimulationNode
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required NodeType NodeType { get; init; }
    public required SimulationStatus Status { get; init; }
    public string? BlockedReason { get; init; }
    public List<ProgramSimulationNode> Children { get; init; } = [];
}

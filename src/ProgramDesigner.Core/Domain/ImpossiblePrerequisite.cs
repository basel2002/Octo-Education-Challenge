namespace ProgramDesigner.Core.Domain;

public sealed record ImpossiblePrerequisite
{
    public required NodeId NodeId { get; init; }
    public required string NodeName { get; init; }
    public required NodeId PrerequisiteId { get; init; }
    public required string PrerequisiteName { get; init; }
    public required ImpossiblePrerequisiteReason Reason { get; init; }
    public required string Description { get; init; }
}

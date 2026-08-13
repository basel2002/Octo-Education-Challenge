namespace ProgramDesigner.Core.Domain;

public sealed record ImpossiblePrerequisite
{
    public required Guid NodeId { get; init; }
    public required string NodeName { get; init; }
    public required Guid PrerequisiteId { get; init; }
    public required string PrerequisiteName { get; init; }
    public required ImpossiblePrerequisiteReason Reason { get; init; }
    public required string Description { get; init; }
}

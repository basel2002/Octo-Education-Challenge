namespace ProgramDesigner.Core.Domain;

public sealed record ReachabilityWarning
{
    public required Guid NodeId { get; init; }
    public required string NodeName { get; init; }
    public required Guid PrerequisiteId { get; init; }
    public required string PrerequisiteName { get; init; }
    public required Guid RiskyChoiceGroupId { get; init; }
    public required string RiskyChoiceGroupName { get; init; }
    public required string Description { get; init; }
}

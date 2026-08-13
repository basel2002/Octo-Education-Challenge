namespace ProgramDesigner.Core.Domain;

public sealed record ReachabilityWarning
{
    public required NodeId NodeId { get; init; }
    public required string NodeName { get; init; }
    public required NodeId PrerequisiteId { get; init; }
    public required string PrerequisiteName { get; init; }
    public required NodeId RiskyChoiceGroupId { get; init; }
    public required string RiskyChoiceGroupName { get; init; }
    public required string Description { get; init; }
}

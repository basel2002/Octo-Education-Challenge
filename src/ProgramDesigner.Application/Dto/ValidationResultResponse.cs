namespace ProgramDesigner.Application.Dto;

using System.Text.Json.Serialization;
using ProgramDesigner.Core.Domain;

public sealed record ValidationResultResponse
{
    public required bool IsValid { get; init; }
    public required List<ImpossiblePrerequisiteResponse> ImpossiblePrerequisites { get; init; }
    public required List<ReachabilityWarningResponse> ReachabilityWarnings { get; init; }
}

public sealed record ImpossiblePrerequisiteResponse
{
    public required Guid NodeId { get; init; }
    public required string NodeName { get; init; }
    public required Guid PrerequisiteId { get; init; }
    public required string PrerequisiteName { get; init; }
    public required ImpossiblePrerequisiteReason Reason { get; init; }
    public required string Description { get; init; }
}

public sealed record ReachabilityWarningResponse
{
    public required Guid NodeId { get; init; }
    public required string NodeName { get; init; }
    public required Guid PrerequisiteId { get; init; }
    public required string PrerequisiteName { get; init; }
    public required Guid RiskyChoiceGroupId { get; init; }
    public required string RiskyChoiceGroupName { get; init; }
    public required string Description { get; init; }
}

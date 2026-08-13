namespace ProgramDesigner.Core.Domain;

public enum ImpossiblePrerequisiteReason
{
    SelfReference,
    DescendantReference,
    ForwardReference
}

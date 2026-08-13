namespace ProgramDesigner.Core.Domain;

/// <summary>
/// A leaf node representing one concrete participant activity in a program tree.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="StepNode"/> has no children; it is the terminal unit that a
/// participant must directly complete. Examples of step activities are:
/// attending a classroom session, passing an online test, or submitting a
/// piece of work for review.
/// </para>
/// <para>
/// The <see cref="StepType"/> string is intentionally open-ended (not an
/// enum) so that the system can support new activity types without a schema
/// migration. Conventional values are <c>"session"</c>, <c>"test"</c>, and
/// <c>"submission"</c>.
/// </para>
/// </remarks>
public sealed class StepNode : ProgramNode
{
    /// <inheritdoc/>
    public override NodeType NodeType => NodeType.Step;

    /// <summary>
    /// Categorises the kind of activity this step represents.
    /// </summary>
    /// <remarks>
    /// Conventional values: <c>"session"</c>, <c>"test"</c>, <c>"submission"</c>.
    /// The value is free-form; the domain does not restrict or validate it
    /// beyond requiring it to be non-null.
    /// </remarks>
    public required string StepType { get; init; }
}

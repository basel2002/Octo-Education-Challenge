namespace ProgramDesigner.Core.Domain;

/// <summary>
/// A container node that holds an ordered list of child <see cref="ProgramNode"/>
/// entries and defines how those children must be completed.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="GroupNode"/> is the only way to create hierarchy in a program
/// tree. Children may themselves be <see cref="GroupNode"/> instances, giving
/// the tree arbitrary depth:
/// <code>
///   GroupNode (InOrder)
///     ├─ StepNode            ← leaf
///     └─ GroupNode (Choice, pick 1)
///          ├─ StepNode       ← leaf
///          └─ GroupNode (InOrder)
///               └─ StepNode  ← leaf at depth 3
/// </code>
/// </para>
/// <para>
/// The <see cref="GroupRule"/> property selects the completion semantics:
/// <list type="bullet">
///   <item>
///     <term><see cref="Domain.GroupRule.InOrder"/></term>
///     <description>Every child must be completed in list order.</description>
///   </item>
///   <item>
///     <term><see cref="Domain.GroupRule.Choice"/></term>
///     <description>
///       The participant completes exactly <see cref="PickCount"/> of the
///       available children (any order). <see cref="PickCount"/> must be
///       between 1 and <c>Children.Count</c> inclusive; this invariant is
///       enforced by <see cref="ValidateInvariants"/>.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
public sealed class GroupNode : ProgramNode
{
    /// <inheritdoc/>
    public override NodeType NodeType => NodeType.Group;

    /// <summary>
    /// Determines how children must be completed by the participant.
    /// See <see cref="Domain.GroupRule"/> for full semantics.
    /// </summary>
    public required GroupRule GroupRule { get; init; }

    /// <summary>
    /// The number of children the participant must complete when
    /// <see cref="GroupRule"/> is <see cref="Domain.GroupRule.Choice"/>.
    /// Must be between 1 and <c>Children.Count</c> inclusive.
    /// Ignored (and should be <c>null</c>) when <see cref="GroupRule"/>
    /// is <see cref="Domain.GroupRule.InOrder"/>.
    /// </summary>
    public int? PickCount { get; init; }

    /// <summary>
    /// Ordered list of child nodes. May contain any mix of
    /// <see cref="StepNode"/> and <see cref="GroupNode"/> instances, allowing
    /// the tree to nest to arbitrary depth.
    /// </summary>
    /// <remarks>
    /// The list is initialised to an empty list so that object-initialiser
    /// syntax can use collection expressions directly, and so that
    /// deserialised nodes with zero children remain valid objects.
    /// </remarks>
    public List<ProgramNode> Children { get; init; } = [];

    // ─────────────────────────────────────────────────────────────────────
    // Structural invariants
    // Note: guards throw immediately on violation rather than collecting
    // errors — full validation (returning structured error lists) is a
    // later story. These guards prevent grossly invalid objects from
    // silently propagating through the system.
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Asserts that this node's structural invariants hold:
    /// <list type="bullet">
    ///   <item>
    ///     A <see cref="Domain.GroupRule.Choice"/> group must have a
    ///     <see cref="PickCount"/> between 1 and <c>Children.Count</c>.
    ///   </item>
    ///   <item>
    ///     A <see cref="Domain.GroupRule.InOrder"/> group must <em>not</em>
    ///     have a <see cref="PickCount"/> set (it is meaningless there).
    ///   </item>
    ///   <item>
    ///     The group must have at least one child.
    ///   </item>
    /// </list>
    /// Does <em>not</em> recurse into children — call
    /// <see cref="EducationProgram.ValidateInvariants"/> to validate the whole
    /// tree in one pass.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any invariant is violated.
    /// </exception>
    public void ValidateInvariants()
    {
        if (Children.Count == 0)
            throw new InvalidOperationException(
                $"GroupNode '{Name}' (Id={Id}) must have at least one child.");

        if (GroupRule == GroupRule.Choice)
        {
            if (PickCount is null)
                throw new InvalidOperationException(
                    $"GroupNode '{Name}' (Id={Id}) has GroupRule=Choice but PickCount is null. " +
                    "Set PickCount to a value between 1 and Children.Count.");

            if (PickCount < 1 || PickCount > Children.Count)
                throw new InvalidOperationException(
                    $"GroupNode '{Name}' (Id={Id}) has GroupRule=Choice with PickCount={PickCount}, " +
                    $"but it must be between 1 and Children.Count ({Children.Count}).");
        }
        else // InOrder
        {
            if (PickCount is not null)
                throw new InvalidOperationException(
                    $"GroupNode '{Name}' (Id={Id}) has GroupRule=InOrder but PickCount={PickCount} " +
                    "is set. PickCount is only meaningful for Choice groups; set it to null.");
        }
    }
}

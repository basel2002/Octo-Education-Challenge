namespace ProgramDesigner.Core.Domain;

/// <summary>
/// Aggregate root for a program — the top-level object that the API creates,
/// stores, retrieves, and validates.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="EducationProgram"/> is essentially named metadata wrapped
/// around a single <see cref="GroupNode"/> root. The root group defines the
/// entire participant experience; its children (and their children, recursively)
/// are <see cref="StepNode"/> and nested <see cref="GroupNode"/> objects.
/// </para>
/// <para>
/// Example tree:
/// <code>
///   EducationProgram  "Leadership Certificate"
///     └─ RootGroup  (InOrder)
///          ├─ StepNode "Orientation"  (session)
///          └─ GroupNode "Electives"  (Choice, pick 2)
///               ├─ StepNode "Finance 101"  (session)
///               └─ GroupNode "Advanced Track"  (InOrder)
///                    ├─ StepNode "Advanced Finance"  (test)
///                    └─ StepNode "Case Study"  (submission)
/// </code>
/// </para>
/// <para>
/// Persistence, API DTOs, and endpoint logic all treat this class as their
/// primary unit — there is no separate concept of "Program" vs "tree"; they
/// are the same object.
/// </para>
/// </remarks>
public sealed class EducationProgram
{
    /// <summary>
    /// Unique, stable identifier for this program. Generated on creation;
    /// used by the API as the resource ID in URIs (e.g. <c>GET /programs/{id}</c>).
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable program name shown in listings and on the program detail page.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The top-level container node. Every other node in the program tree is
    /// a direct or indirect descendant of this group.
    /// </summary>
    /// <remarks>
    /// The root is always a <see cref="GroupNode"/> (never a bare
    /// <see cref="StepNode"/>) because a program with a single step would
    /// be trivially modelled as a one-child InOrder group.
    /// </remarks>
    public required GroupNode RootGroup { get; init; }

    // ─────────────────────────────────────────────────────────────────────
    // Structural invariants
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Recursively validates structural invariants across the entire program
    /// tree by performing a depth-first traversal and calling
    /// <see cref="GroupNode.ValidateInvariants"/> on every
    /// <see cref="GroupNode"/> encountered.
    /// </summary>
    /// <remarks>
    /// This method validates only <em>structural</em> rules (e.g. Choice groups
    /// must have a valid PickCount). Cross-node rules such as prerequisite
    /// reachability and cycle detection are handled by dedicated validator
    /// logic in a later story.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any node in the tree violates its structural invariants.
    /// The message identifies the offending node by name and Id.
    /// </exception>
    public void ValidateInvariants()
    {
        ValidateGroupNodeRecursive(RootGroup);
    }

    // ── private helpers ──────────────────────────────────────────────────

    private static void ValidateGroupNodeRecursive(GroupNode group)
    {
        group.ValidateInvariants();

        foreach (var child in group.Children)
        {
            if (child is GroupNode childGroup)
                ValidateGroupNodeRecursive(childGroup);
        }
    }
}

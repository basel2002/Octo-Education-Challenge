using System.Text.Json.Serialization;

namespace ProgramDesigner.Core.Domain;

// ─────────────────────────────────────────────────────────────────────────────
// Design note — why classes, not records?
//
// Records are ideal when objects are compared by value and created once with
// all required data. ProgramNode objects, however:
//   • are identified by Id (Guid), not by structural equality
//   • contain a mutable Children list (GroupNode) that grows during tree
//     construction and deserialization
//   • have many optional nullable fields (PrerequisiteId, PickCount) that
//     don't fit neatly into positional record syntax
//
// Using plain sealed classes + init-only properties gives us clean
// construction via object initialisers, natural JSON round-tripping via
// System.Text.Json, and correct reference semantics for tree identity.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Abstract base for every node in a program tree.
/// </summary>
/// <remarks>
/// <para>
/// The tree is a heterogeneous recursive structure:
/// <code>
///   EducationProgram
///     └─ RootGroup  (GroupNode)
///          ├─ StepNode
///          └─ GroupNode
///               ├─ StepNode
///               └─ GroupNode
///                    └─ StepNode     ← arbitrary depth
/// </code>
/// </para>
/// <para>
/// Every node carries a stable <see cref="Id"/> (Guid) that is used by
/// <see cref="PrerequisiteId"/> references anywhere else in the same tree.
/// </para>
/// <para>
/// JSON polymorphism is handled by <c>System.Text.Json</c>'s native
/// <see cref="JsonPolymorphicAttribute"/> support. The discriminator field is
/// <c>"nodeType"</c>; its values are <c>"step"</c> and <c>"group"</c>.
/// This means any serialised tree can be deserialized back to the correct
/// concrete type without a custom converter.
/// </para>
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "nodeType")]
[JsonDerivedType(typeof(StepNode),  typeDiscriminator: "step")]
[JsonDerivedType(typeof(GroupNode), typeDiscriminator: "group")]
public abstract class ProgramNode
{
    /// <summary>
    /// Unique, stable identifier for this node within its program tree.
    /// Used by <see cref="PrerequisiteId"/> references.
    /// </summary>
    public NodeId Id { get; init; } = NodeId.NewNodeId();

    /// <summary>
    /// Human-readable label shown to the program designer and participants.
    /// Must not be null or empty (enforced by <see cref="ValidateInvariants"/>
    /// on the owning <see cref="EducationProgram"/>).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Discriminates between <see cref="StepNode"/> and <see cref="GroupNode"/>.
    /// Always matches the concrete derived type and the JSON <c>"nodeType"</c>
    /// discriminator value.
    /// </summary>
    /// <remarks>
    /// Decorated with <see cref="JsonIgnoreAttribute"/> because <c>System.Text.Json</c>
    /// already emits the <c>"nodeType"</c> discriminator string (configured via
    /// <see cref="JsonPolymorphicAttribute"/>). Serialising this property as well would
    /// produce a redundant <c>"NodeType": 0</c> integer alongside the discriminator,
    /// confusing consumers.
    /// </remarks>
    [JsonIgnore]
    public abstract NodeType NodeType { get; }

    /// <summary>
    /// Optional reference to another node's <see cref="Id"/> in the same
    /// program tree. When set, the referenced node must be completed before
    /// this node (and everything inside it, if this is a <see cref="GroupNode"/>)
    /// becomes available to the participant.
    /// </summary>
    /// <remarks>
    /// Validity of the reference (i.e. that the Id actually exists in the tree,
    /// and does not create a cycle) is validated by dedicated prerequisite-
    /// validation logic in a later story — not here.
    /// </remarks>
    public NodeId? PrerequisiteId { get; init; }
}

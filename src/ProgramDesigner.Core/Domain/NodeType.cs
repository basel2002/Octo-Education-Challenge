namespace ProgramDesigner.Core.Domain;

/// <summary>
/// Identifies whether a <see cref="ProgramNode"/> is a leaf activity
/// (<see cref="Step"/>) or a structural container (<see cref="Group"/>).
/// This value is always serialised as the JSON discriminator field
/// <c>"nodeType"</c> so consumers can determine the concrete type
/// without inspecting other fields.
/// </summary>
public enum NodeType
{
    /// <summary>A leaf node that represents one concrete participant activity.</summary>
    Step = 0,

    /// <summary>A container node that holds child <see cref="ProgramNode"/> entries.</summary>
    Group = 1
}

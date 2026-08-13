namespace ProgramDesigner.Core.Domain;

/// <summary>
/// Controls how a <see cref="GroupNode"/> evaluates participant completion.
/// </summary>
public enum GroupRule
{
    /// <summary>
    /// Every child must be completed, in the order they appear in
    /// <see cref="GroupNode.Children"/>. A participant cannot start child N+1
    /// until child N is marked complete (enforcement is the responsibility of
    /// the runtime, not the domain model).
    /// </summary>
    InOrder = 0,

    /// <summary>
    /// The participant must complete exactly <see cref="GroupNode.PickCount"/>
    /// children out of the total available. The chosen children may be
    /// completed in any order. <see cref="GroupNode.PickCount"/> must be
    /// between 1 and <c>Children.Count</c> inclusive.
    /// </summary>
    Choice = 1
}

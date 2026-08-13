using System.Text.Json;
using ProgramDesigner.Core.Domain;
using Xunit;

namespace ProgramDesigner.Tests.Domain;

/// <summary>
/// Verifies that the program tree can be serialised to JSON and deserialised
/// back to the correct concrete types without losing any structural information.
/// </summary>
/// <remarks>
/// This is the most critical property of the domain model: because the tree
/// is polymorphic (<see cref="ProgramNode"/> with <see cref="StepNode"/> and
/// <see cref="GroupNode"/> subtypes), the JSON round-trip must preserve the
/// concrete type of every node so that nesting, rules, and step-types are
/// all recoverable from a stored or transmitted JSON payload.
/// </remarks>
public sealed class ProgramTreeJsonTests
{
    // ── Shared JsonSerializerOptions ─────────────────────────────────────
    // System.Text.Json resolves [JsonPolymorphic] / [JsonDerivedType]
    // attributes automatically; no custom converters are needed.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    // ─────────────────────────────────────────────────────────────────────
    // Helper: build a representative 3-level-deep program
    //
    //  EducationProgram  "Leadership Certificate"
    //    └─ RootGroup  (InOrder)             ← level 1
    //         ├─ StepNode "Orientation"      ← level 2 — leaf, stepType=session
    //         └─ ChoiceGroup (Choice, pick 1)← level 2 — group
    //              ├─ StepNode "Finance 101" ← level 3 — leaf, stepType=session
    //              └─ InnerGroup (InOrder)   ← level 3 — group
    //                   └─ StepNode "Case Study"  ← level 4 — leaf, stepType=submission
    //
    // The Orientation step has a PrerequisiteId pointing at itself for
    // simplicity in this unit test — real prerequisite validation is a
    // later story; here we just verify the Guid survives the round-trip.
    // ─────────────────────────────────────────────────────────────────────
    private static EducationProgram BuildSampleProgram()
    {
        var orientationId = Guid.NewGuid();

        var innerGroup = new GroupNode
        {
            Name     = "Advanced Track",
            GroupRule = GroupRule.InOrder,
            Children =
            [
                new StepNode
                {
                    Name     = "Case Study",
                    StepType = "submission"
                }
            ]
        };

        var choiceGroup = new GroupNode
        {
            Name      = "Electives",
            GroupRule  = GroupRule.Choice,
            PickCount  = 1,
            Children  =
            [
                new StepNode
                {
                    Name     = "Finance 101",
                    StepType = "session"
                },
                innerGroup
            ]
        };

        var orientationStep = new StepNode
        {
            Id       = orientationId,
            Name     = "Orientation",
            StepType = "session"
        };

        // The ChoiceGroup requires Orientation to be done first.
        var choiceGroupWithPrereq = new GroupNode
        {
            Id             = choiceGroup.Id,
            Name           = choiceGroup.Name,
            GroupRule       = choiceGroup.GroupRule,
            PickCount       = choiceGroup.PickCount,
            PrerequisiteId  = orientationId,   // ← prerequisite wired here
            Children       = choiceGroup.Children
        };

        var rootGroup = new GroupNode
        {
            Name      = "Root",
            GroupRule  = GroupRule.InOrder,
            Children  = [orientationStep, choiceGroupWithPrereq]
        };

        return new EducationProgram
        {
            Name      = "Leadership Certificate",
            RootGroup = rootGroup
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 1 — full round-trip: serialise → deserialise → structural check
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesEntireProgramStructure()
    {
        // Arrange
        var original = BuildSampleProgram();

        // Act
        var json       = JsonSerializer.Serialize(original, JsonOptions);
        var deserialised = JsonSerializer.Deserialize<EducationProgram>(json, JsonOptions);

        // Assert — top-level program
        Assert.NotNull(deserialised);
        Assert.Equal(original.Id,   deserialised.Id);
        Assert.Equal(original.Name, deserialised.Name);

        // Assert — root group (level 1)
        var root = deserialised.RootGroup;
        Assert.IsType<GroupNode>(root);
        Assert.Equal(GroupRule.InOrder, root.GroupRule);
        Assert.Null(root.PickCount);
        Assert.Equal(2, root.Children.Count);

        // Assert — level 2, child 0: Orientation step
        var orientation = Assert.IsType<StepNode>(root.Children[0]);
        Assert.Equal("Orientation", orientation.Name);
        Assert.Equal("session",     orientation.StepType);
        Assert.Equal(NodeType.Step, orientation.NodeType);
        Assert.Null(orientation.PrerequisiteId);

        // Assert — level 2, child 1: Electives choice group
        var electives = Assert.IsType<GroupNode>(root.Children[1]);
        Assert.Equal("Electives",       electives.Name);
        Assert.Equal(GroupRule.Choice,  electives.GroupRule);
        Assert.Equal(1,                 electives.PickCount);
        Assert.Equal(NodeType.Group,    electives.NodeType);

        // PrerequisiteId must survive the round-trip exactly
        Assert.Equal(orientation.Id, electives.PrerequisiteId);

        // Assert — level 3, child 0 of Electives: Finance 101 step
        var finance101 = Assert.IsType<StepNode>(electives.Children[0]);
        Assert.Equal("Finance 101", finance101.Name);
        Assert.Equal("session",     finance101.StepType);

        // Assert — level 3, child 1 of Electives: Advanced Track group
        var advancedTrack = Assert.IsType<GroupNode>(electives.Children[1]);
        Assert.Equal("Advanced Track",   advancedTrack.Name);
        Assert.Equal(GroupRule.InOrder,  advancedTrack.GroupRule);
        Assert.Null(advancedTrack.PickCount);
        Assert.Single(advancedTrack.Children);

        // Assert — level 4: Case Study step
        var caseStudy = Assert.IsType<StepNode>(advancedTrack.Children[0]);
        Assert.Equal("Case Study",  caseStudy.Name);
        Assert.Equal("submission",  caseStudy.StepType);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 2 — JSON discriminator is present and correct
    //
    // STJ emits the "nodeType" discriminator only when a value is serialised
    // through the *abstract* base-type reference (ProgramNode). That happens
    // for every element inside a List<ProgramNode> (i.e. Children arrays).
    // The RootGroup property is typed as GroupNode (concrete), so it does NOT
    // carry the discriminator — but its Children elements do, because they are
    // typed as List<ProgramNode>.
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Serialise_EmitsNodeTypeDiscriminator()
    {
        // Arrange
        var program = BuildSampleProgram();

        // Act
        var json = JsonSerializer.Serialize(program, JsonOptions);
        using var doc = JsonDocument.Parse(json);

        // Root group children are serialised as List<ProgramNode> → discriminator present.
        // Note: STJ uses PascalCase for property names ("RootGroup", "Children") by default,
        // but the discriminator field name is "nodeType" (lowercase) as configured in [JsonPolymorphic].
        var rootChildren = doc.RootElement
            .GetProperty("RootGroup")
            .GetProperty("Children");

        // children[0] = Orientation StepNode → nodeType: "step"
        Assert.Equal("step", rootChildren[0].GetProperty("nodeType").GetString());

        // children[1] = Electives GroupNode → nodeType: "group"
        Assert.Equal("group", rootChildren[1].GetProperty("nodeType").GetString());

        // children[1].children[1] = Advanced Track GroupNode → nodeType: "group"
        var electivesChildren = rootChildren[1].GetProperty("Children");
        Assert.Equal("group", electivesChildren[1].GetProperty("nodeType").GetString());

        // children[1].children[0] = Finance 101 StepNode → nodeType: "step"
        Assert.Equal("step", electivesChildren[0].GetProperty("nodeType").GetString());
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 3 — ValidateInvariants passes on the well-formed sample
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateInvariants_DoesNotThrow_ForWellFormedProgram()
    {
        var program = BuildSampleProgram();
        var exception = Record.Exception(program.ValidateInvariants);
        Assert.Null(exception);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 4 — guard: Choice group with PickCount > Children.Count
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateInvariants_Throws_WhenChoicePickCountExceedsChildCount()
    {
        var program = new EducationProgram
        {
            Name = "Bad Program",
            RootGroup = new GroupNode
            {
                Name      = "Root",
                GroupRule  = GroupRule.Choice,
                PickCount  = 5,              // 5 out of 2 children — invalid
                Children  =
                [
                    new StepNode { Name = "A", StepType = "test" },
                    new StepNode { Name = "B", StepType = "test" }
                ]
            }
        };

        Assert.Throws<InvalidOperationException>(program.ValidateInvariants);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 5 — guard: Choice group with null PickCount
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateInvariants_Throws_WhenChoiceGroupHasNullPickCount()
    {
        var program = new EducationProgram
        {
            Name = "Bad Program",
            RootGroup = new GroupNode
            {
                Name      = "Root",
                GroupRule  = GroupRule.Choice,
                PickCount  = null,           // missing — invalid for Choice
                Children  =
                [
                    new StepNode { Name = "A", StepType = "test" }
                ]
            }
        };

        Assert.Throws<InvalidOperationException>(program.ValidateInvariants);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 6 — guard: InOrder group must not have PickCount set
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateInvariants_Throws_WhenInOrderGroupHasPickCount()
    {
        var program = new EducationProgram
        {
            Name = "Bad Program",
            RootGroup = new GroupNode
            {
                Name      = "Root",
                GroupRule  = GroupRule.InOrder,
                PickCount  = 1,              // nonsensical for InOrder
                Children  =
                [
                    new StepNode { Name = "A", StepType = "session" }
                ]
            }
        };

        Assert.Throws<InvalidOperationException>(program.ValidateInvariants);
    }
}

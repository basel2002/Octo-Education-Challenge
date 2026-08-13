namespace ProgramDesigner.Tests.Validators;

using System;
using System.Linq;
using ProgramDesigner.Core.Domain;
using ProgramDesigner.Core.Validators;
using Xunit;

public class PrerequisiteValidatorTests
{
    private readonly PrerequisiteValidator _validator = new();

    [Fact]
    public void FindImpossiblePrerequisites_DescendantReference_ReturnsDescendantReferenceError()
    {
        // Arrange
        var stepId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var program = new EducationProgram
        {
            Id = Guid.NewGuid(),
            Name = "Program",
            RootGroup = new GroupNode
            {
                Id = Guid.NewGuid(),
                Name = "Root",
                GroupRule = GroupRule.InOrder,
                Children =
                [
                    new GroupNode
                    {
                        Id = groupId,
                        Name = "Group",
                        GroupRule = GroupRule.InOrder,
                        PrerequisiteId = stepId,
                        Children =
                        [
                            new StepNode { Id = stepId, Name = "Child Step", StepType = "session" }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _validator.FindImpossiblePrerequisites(program);

        // Assert
        Assert.Single(result);
        Assert.Equal(ImpossiblePrerequisiteReason.DescendantReference, result[0].Reason);
        Assert.Equal(groupId, result[0].NodeId);
        Assert.Equal(stepId, result[0].PrerequisiteId);
    }

    [Fact]
    public void FindImpossiblePrerequisites_ForwardReference_ReturnsForwardReferenceError()
    {
        // Arrange
        var step1Id = Guid.NewGuid();
        var step2Id = Guid.NewGuid();
        var program = new EducationProgram
        {
            Id = Guid.NewGuid(),
            Name = "Program",
            RootGroup = new GroupNode
            {
                Id = Guid.NewGuid(),
                Name = "Root",
                GroupRule = GroupRule.InOrder,
                Children =
                [
                    new StepNode { Id = step1Id, Name = "Step 1", StepType = "session", PrerequisiteId = step2Id },
                    new StepNode { Id = step2Id, Name = "Step 2", StepType = "session" }
                ]
            }
        };

        // Act
        var result = _validator.FindImpossiblePrerequisites(program);

        // Assert
        Assert.Single(result);
        Assert.Equal(ImpossiblePrerequisiteReason.ForwardReference, result[0].Reason);
        Assert.Equal(step1Id, result[0].NodeId);
        Assert.Equal(step2Id, result[0].PrerequisiteId);
    }

    [Fact]
    public void FindImpossiblePrerequisites_ValidBackwardReference_ReturnsEmpty()
    {
        // Arrange
        var step1Id = Guid.NewGuid();
        var step2Id = Guid.NewGuid();
        var program = new EducationProgram
        {
            Id = Guid.NewGuid(),
            Name = "Program",
            RootGroup = new GroupNode
            {
                Id = Guid.NewGuid(),
                Name = "Root",
                GroupRule = GroupRule.InOrder,
                Children =
                [
                    new StepNode { Id = step1Id, Name = "Step 1", StepType = "session" },
                    new StepNode { Id = step2Id, Name = "Step 2", StepType = "session", PrerequisiteId = step1Id }
                ]
            }
        };

        // Act
        var result = _validator.FindImpossiblePrerequisites(program);

        // Assert
        Assert.Empty(result);
    }

}

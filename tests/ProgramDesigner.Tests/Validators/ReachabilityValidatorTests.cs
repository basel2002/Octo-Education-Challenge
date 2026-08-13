namespace ProgramDesigner.Tests.Validators;

using System;
using System.Linq;
using ProgramDesigner.Core.Domain;
using ProgramDesigner.Core.Validators;
using Xunit;

public class ReachabilityValidatorTests
{
    private readonly ReachabilityValidator _validator = new();

    [Fact]
    public void FindReachabilityWarnings_TargetIsChoiceGroup_ReturnsEmpty()
    {
        // Final Capstone -> Major
        var majorId = Guid.NewGuid();
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
                        Id = majorId,
                        Name = "Major",
                        GroupRule = GroupRule.Choice,
                        PickCount = 1,
                        Children = [ new StepNode { Id = Guid.NewGuid(), Name = "AI", StepType = "session" } ]
                    },
                    new StepNode
                    {
                        Id = Guid.NewGuid(),
                        Name = "Final Capstone",
                        StepType = "submission",
                        PrerequisiteId = majorId
                    }
                ]
            }
        };

        var warnings = _validator.FindReachabilityWarnings(program);
        Assert.Empty(warnings);
    }

    [Fact]
    public void FindReachabilityWarnings_TargetInsideChoiceGroup_SourceOutside_ReturnsWarning()
    {
        // Final Capstone -> AI Capstone
        var majorId = Guid.NewGuid();
        var aiCapstoneId = Guid.NewGuid();
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
                        Id = majorId,
                        Name = "Major",
                        GroupRule = GroupRule.Choice,
                        PickCount = 1,
                        Children =
                        [
                            new GroupNode
                            {
                                Id = Guid.NewGuid(),
                                Name = "AI",
                                GroupRule = GroupRule.InOrder,
                                Children = [ new StepNode { Id = aiCapstoneId, Name = "AI Capstone", StepType = "session" } ]
                            },
                            new GroupNode { Id = Guid.NewGuid(), Name = "IT", GroupRule = GroupRule.InOrder }
                        ]
                    },
                    new StepNode
                    {
                        Id = Guid.NewGuid(),
                        Name = "Final Capstone",
                        StepType = "submission",
                        PrerequisiteId = aiCapstoneId
                    }
                ]
            }
        };

        var warnings = _validator.FindReachabilityWarnings(program);
        
        Assert.Single(warnings);
        Assert.Equal(majorId, warnings[0].RiskyChoiceGroupId);
        Assert.Equal("Major", warnings[0].RiskyChoiceGroupName);
    }

    [Fact]
    public void FindReachabilityWarnings_TargetInsideInOrderGroup_ReturnsEmpty()
    {
        var targetId = Guid.NewGuid();
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
                        Id = Guid.NewGuid(),
                        Name = "InOrder Group",
                        GroupRule = GroupRule.InOrder,
                        Children = [ new StepNode { Id = targetId, Name = "Target", StepType = "session" } ]
                    },
                    new StepNode
                    {
                        Id = Guid.NewGuid(),
                        Name = "Source",
                        StepType = "session",
                        PrerequisiteId = targetId
                    }
                ]
            }
        };

        var warnings = _validator.FindReachabilityWarnings(program);
        Assert.Empty(warnings);
    }

    [Fact]
    public void FindReachabilityWarnings_TargetInsideNestedChoice_SourceSharesOuterChoice_ReturnsWarningForInnerChoice()
    {
        // Source is AI Capstone (inside AI branch of Major).
        // Target is Cybersecurity (inside Electives Choice branch of AI branch of Major).
        var electivesId = Guid.NewGuid();
        var cybersecurityId = Guid.NewGuid();
        
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
                        Id = Guid.NewGuid(),
                        Name = "Major",
                        GroupRule = GroupRule.Choice,
                        PickCount = 1,
                        Children =
                        [
                            new GroupNode
                            {
                                Id = Guid.NewGuid(),
                                Name = "AI",
                                GroupRule = GroupRule.InOrder,
                                Children =
                                [
                                    new GroupNode
                                    {
                                        Id = electivesId,
                                        Name = "Electives",
                                        GroupRule = GroupRule.Choice,
                                        PickCount = 2,
                                        Children =
                                        [
                                            new StepNode { Id = cybersecurityId, Name = "Cybersecurity", StepType = "session" },
                                            new StepNode { Id = Guid.NewGuid(), Name = "Other 1", StepType = "session" },
                                            new StepNode { Id = Guid.NewGuid(), Name = "Other 2", StepType = "session" }
                                        ]
                                    },
                                    new StepNode 
                                    { 
                                        Id = Guid.NewGuid(), 
                                        Name = "AI Capstone", 
                                        StepType = "submission", 
                                        PrerequisiteId = cybersecurityId 
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var warnings = _validator.FindReachabilityWarnings(program);
        
        Assert.Single(warnings);
        Assert.Equal(electivesId, warnings[0].RiskyChoiceGroupId);
        Assert.Equal("Electives", warnings[0].RiskyChoiceGroupName);
    }
}

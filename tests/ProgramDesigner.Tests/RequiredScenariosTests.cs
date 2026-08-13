namespace ProgramDesigner.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using ProgramDesigner.Api.Dto;
using ProgramDesigner.Core.Domain;
using ProgramDesigner.Core.Services;
using ProgramDesigner.Core.Validators;

public class RequiredScenariosTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public async Task FullComputerScienceScenario_ValidatesWithoutErrorsOrWarnings()
    {
        // Challenge requirement: full Computer Science structure validates with no impossible prerequisites or reachability warnings.
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/programs", BuildFullComputerScienceRequest(), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var createdProgram = await ReadJsonAsync<ProgramResponse>(postResponse);
        AssertFullComputerScienceStructure(createdProgram);

        var validateResponse = await client.PostAsync($"/programs/{createdProgram.Id}/validate", null);
        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);

        var validationResult = await ReadJsonAsync<ValidationResultResponse>(validateResponse);
        Assert.True(validationResult.IsValid);
        Assert.Empty(validationResult.ImpossiblePrerequisites);
        Assert.Empty(validationResult.ReachabilityWarnings);
    }

    [Fact]
    public void DirectPrerequisiteCycle_IsRejected()
    {
        // Challenge requirement: direct A <-> B prerequisite cycle is rejected as an impossible prerequisite.
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var program = new EducationProgram
        {
            Name = "Direct Cycle",
            RootGroup = new GroupNode
            {
                Name = "Root",
                GroupRule = GroupRule.InOrder,
                Children =
                [
                    new StepNode { Id = aId, Name = "A", StepType = "session", PrerequisiteId = bId },
                    new StepNode { Id = bId, Name = "B", StepType = "session", PrerequisiteId = aId }
                ]
            }
        };
        var validator = new ProgramValidationService(new PrerequisiteValidator(), new ReachabilityValidator());

        var (isValid, impossiblePrerequisites, _) = validator.Validate(program);

        Assert.False(isValid);
        var cycleEntry = Assert.Single(impossiblePrerequisites);
        Assert.Equal(aId, cycleEntry.NodeId);
        Assert.Equal(bId, cycleEntry.PrerequisiteId);
        Assert.Equal(ImpossiblePrerequisiteReason.ForwardReference, cycleEntry.Reason);
        Assert.Contains("appears later", cycleEntry.Description);
    }

    [Fact]
    public void PrerequisiteInUnchosenChoicePath_GeneratesWarningNotRejection()
    {
        // Challenge requirement: prerequisite reachable only through an unchosen choice path warns, but does not invalidate.
        var aiCapstoneId = Guid.NewGuid();
        var program = new EducationProgram
        {
            Name = "Unchosen Choice Path",
            RootGroup = new GroupNode
            {
                Name = "Root",
                GroupRule = GroupRule.InOrder,
                Children =
                [
                    new GroupNode
                    {
                        Name = "Major",
                        GroupRule = GroupRule.Choice,
                        PickCount = 1,
                        Children =
                        [
                            new GroupNode
                            {
                                Name = "AI",
                                GroupRule = GroupRule.InOrder,
                                Children =
                                [
                                    new StepNode { Id = aiCapstoneId, Name = "AI Capstone", StepType = "submission" }
                                ]
                            },
                            new GroupNode
                            {
                                Name = "IT",
                                GroupRule = GroupRule.InOrder,
                                Children =
                                [
                                    new StepNode { Name = "Networking", StepType = "session" }
                                ]
                            }
                        ]
                    },
                    new StepNode
                    {
                        Name = "Final Capstone",
                        StepType = "submission",
                        PrerequisiteId = aiCapstoneId
                    }
                ]
            }
        };
        var validator = new ProgramValidationService(new PrerequisiteValidator(), new ReachabilityValidator());

        var (isValid, impossiblePrerequisites, reachabilityWarnings) = validator.Validate(program);

        Assert.True(isValid);
        Assert.Empty(impossiblePrerequisites);
        var warning = Assert.Single(reachabilityWarnings);
        Assert.Equal("Major", warning.RiskyChoiceGroupName);
    }

    [Fact]
    public void SelfReferencingPrerequisite_IsRejected()
    {
        // Challenge requirement: self-referencing prerequisite is rejected with a SelfReference impossible prerequisite.
        var stepId = Guid.NewGuid();
        var program = new EducationProgram
        {
            Name = "Self Reference",
            RootGroup = new GroupNode
            {
                Name = "Root",
                GroupRule = GroupRule.InOrder,
                Children =
                [
                    new StepNode { Id = stepId, Name = "A", StepType = "session", PrerequisiteId = stepId }
                ]
            }
        };
        var validator = new ProgramValidationService(new PrerequisiteValidator(), new ReachabilityValidator());

        var (isValid, impossiblePrerequisites, _) = validator.Validate(program);

        Assert.False(isValid);
        var selfReference = Assert.Single(impossiblePrerequisites);
        Assert.Equal(stepId, selfReference.NodeId);
        Assert.Equal(stepId, selfReference.PrerequisiteId);
        Assert.Equal(ImpossiblePrerequisiteReason.SelfReference, selfReference.Reason);
    }

    private static ProgramCreateRequest BuildFullComputerScienceRequest()
    {
        return new ProgramCreateRequest
        {
            Name = "Computer Science",
            RootGroup = new GroupNodeRequest
            {
                Name = "Computer Science",
                GroupRule = GroupRule.InOrder,
                Children =
                [
                    new GroupNodeRequest
                    {
                        Key = "Foundations",
                        Name = "Foundations",
                        GroupRule = GroupRule.InOrder,
                        Children =
                        [
                            new StepNodeRequest { Name = "Introduction to Computing", StepType = "session" },
                            new StepNodeRequest { Name = "Mathematics for Computing", StepType = "session" }
                        ]
                    },
                    new GroupNodeRequest
                    {
                        Key = "Major",
                        Name = "Major",
                        GroupRule = GroupRule.Choice,
                        PickCount = 1,
                        PrerequisiteRef = "Foundations",
                        Children =
                        [
                            new GroupNodeRequest
                            {
                                Name = "AI",
                                GroupRule = GroupRule.InOrder,
                                Children =
                                [
                                    new StepNodeRequest { Name = "Machine Learning Basics", StepType = "session" },
                                    new GroupNodeRequest
                                    {
                                        Key = "Electives",
                                        Name = "Electives",
                                        GroupRule = GroupRule.Choice,
                                        PickCount = 2,
                                        Children =
                                        [
                                            new StepNodeRequest { Name = "Computer Vision", StepType = "session" },
                                            new StepNodeRequest { Name = "Natural Language Processing", StepType = "session" },
                                            new StepNodeRequest { Name = "Robotics", StepType = "session" }
                                        ]
                                    },
                                    new StepNodeRequest
                                    {
                                        Name = "AI Capstone",
                                        StepType = "submission",
                                        PrerequisiteRef = "Electives"
                                    }
                                ]
                            },
                            new GroupNodeRequest
                            {
                                Name = "IT",
                                GroupRule = GroupRule.InOrder,
                                Children =
                                [
                                    new StepNodeRequest { Name = "Networking Fundamentals", StepType = "session" },
                                    new StepNodeRequest { Name = "Systems Administration", StepType = "test" }
                                ]
                            },
                            new GroupNodeRequest
                            {
                                Name = "Programming",
                                GroupRule = GroupRule.InOrder,
                                Children =
                                [
                                    new StepNodeRequest { Name = "Programming Basics", StepType = "session" },
                                    new StepNodeRequest { Name = "Software Project", StepType = "submission" }
                                ]
                            }
                        ]
                    },
                    new StepNodeRequest
                    {
                        Name = "Final Capstone",
                        StepType = "submission",
                        PrerequisiteRef = "Major"
                    }
                ]
            }
        };
    }

    private static void AssertFullComputerScienceStructure(ProgramResponse program)
    {
        Assert.Equal("Computer Science", program.Name);
        Assert.Equal(3, program.RootGroup.Children.Count);

        var foundations = Assert.IsType<GroupNodeResponse>(program.RootGroup.Children[0]);
        Assert.Equal(GroupRule.InOrder, foundations.GroupRule);
        Assert.Collection(
            foundations.Children,
            node => Assert.Equal("Introduction to Computing", Assert.IsType<StepNodeResponse>(node).Name),
            node => Assert.Equal("Mathematics for Computing", Assert.IsType<StepNodeResponse>(node).Name));

        var major = Assert.IsType<GroupNodeResponse>(program.RootGroup.Children[1]);
        Assert.Equal(GroupRule.Choice, major.GroupRule);
        Assert.Equal(1, major.PickCount);
        Assert.Equal(foundations.Id, major.PrerequisiteId);

        var ai = Assert.IsType<GroupNodeResponse>(major.Children[0]);
        Assert.Equal(GroupRule.InOrder, ai.GroupRule);
        Assert.Equal("Machine Learning Basics", Assert.IsType<StepNodeResponse>(ai.Children[0]).Name);

        var electives = Assert.IsType<GroupNodeResponse>(ai.Children[1]);
        Assert.Equal(GroupRule.Choice, electives.GroupRule);
        Assert.Equal(2, electives.PickCount);
        Assert.Collection(
            electives.Children,
            node => Assert.Equal("Computer Vision", Assert.IsType<StepNodeResponse>(node).Name),
            node => Assert.Equal("Natural Language Processing", Assert.IsType<StepNodeResponse>(node).Name),
            node => Assert.Equal("Robotics", Assert.IsType<StepNodeResponse>(node).Name));

        var aiCapstone = Assert.IsType<StepNodeResponse>(ai.Children[2]);
        Assert.Equal("AI Capstone", aiCapstone.Name);
        Assert.Equal(electives.Id, aiCapstone.PrerequisiteId);

        var it = Assert.IsType<GroupNodeResponse>(major.Children[1]);
        Assert.Equal(2, it.Children.Count);
        var programming = Assert.IsType<GroupNodeResponse>(major.Children[2]);
        Assert.Equal(2, programming.Children.Count);

        var finalCapstone = Assert.IsType<StepNodeResponse>(program.RootGroup.Children[2]);
        Assert.Equal("Final Capstone", finalCapstone.Name);
        Assert.Equal(major.Id, finalCapstone.PrerequisiteId);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var value = JsonSerializer.Deserialize<T>(content, JsonOptions);
        Assert.NotNull(value);
        return value;
    }
}

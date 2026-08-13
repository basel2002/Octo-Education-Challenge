namespace ProgramDesigner.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using ProgramDesigner.Api.Dto;
using ProgramDesigner.Core.Domain;

public class ProgramSimulationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public async Task SimulateProgram_ComputerScienceAiChoice_ReturnsExpectedProgressTree()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var createdProgram = await CreateComputerScienceProgram(client);

        var foundations = FindGroup(createdProgram.RootGroup, "Foundations");
        var major = FindGroup(createdProgram.RootGroup, "Major");
        var ai = FindGroup(createdProgram.RootGroup, "AI");
        var completedFoundationsSteps = foundations.Children
            .Select(child => Assert.IsType<StepNodeResponse>(child).Id)
            .ToList();

        var request = new ProgramSimulationRequest
        {
            Choices = new Dictionary<Guid, List<Guid>>
            {
                [major.Id] = [ai.Id]
            },
            CompletedStepIds = completedFoundationsSteps
        };

        var response = await client.PostAsJsonAsync($"/programs/{createdProgram.Id}/simulate", request, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var simulation = await ReadJsonAsync<ProgramSimulationResponse>(response);

        Assert.Equal(SimulationStatus.Unlocked, simulation.RootNode.Status);

        var foundationsStatus = FindNode(simulation.RootNode, "Foundations");
        Assert.Equal(SimulationStatus.Complete, foundationsStatus.Status);

        var majorStatus = FindNode(simulation.RootNode, "Major");
        Assert.Equal(SimulationStatus.Unlocked, majorStatus.Status);

        var aiStatus = FindNode(simulation.RootNode, "AI");
        Assert.Equal(SimulationStatus.Unlocked, aiStatus.Status);

        var machineLearningStatus = FindNode(simulation.RootNode, "Machine Learning Basics");
        Assert.Equal(SimulationStatus.Unlocked, machineLearningStatus.Status);

        AssertNotChosen(simulation.RootNode, "IT");
        AssertNotChosen(simulation.RootNode, "Programming");

        var finalCapstoneStatus = FindNode(simulation.RootNode, "Final Capstone");
        Assert.Equal(SimulationStatus.Blocked, finalCapstoneStatus.Status);
        Assert.Equal("Blocked: prerequisite 'Major' not yet complete.", finalCapstoneStatus.BlockedReason);
    }

    [Fact]
    public async Task SimulateProgram_ChoiceReferencesNonChild_Returns400BadRequest()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var createdProgram = await CreateComputerScienceProgram(client);
        var major = FindGroup(createdProgram.RootGroup, "Major");

        var request = new ProgramSimulationRequest
        {
            Choices = new Dictionary<Guid, List<Guid>>
            {
                [major.Id] = [Guid.NewGuid()]
            }
        };

        var response = await client.PostAsJsonAsync($"/programs/{createdProgram.Id}/simulate", request, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("does not contain child id", content);
    }

    private static async Task<ProgramResponse> CreateComputerScienceProgram(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/programs", BuildComputerScienceRequest(), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadJsonAsync<ProgramResponse>(response);
    }

    private static ProgramCreateRequest BuildComputerScienceRequest()
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

    private static GroupNodeResponse FindGroup(GroupNodeResponse root, string name)
    {
        return Assert.IsType<GroupNodeResponse>(FindNode(root, name));
    }

    private static ProgramNodeResponse FindNode(ProgramNodeResponse node, string name)
    {
        if (node.Name == name)
        {
            return node;
        }

        if (node is GroupNodeResponse group)
        {
            foreach (var child in group.Children)
            {
                var match = TryFindNode(child, name);
                if (match != null)
                {
                    return match;
                }
            }
        }

        throw new InvalidOperationException($"Node '{name}' was not found.");
    }

    private static ProgramNodeResponse? TryFindNode(ProgramNodeResponse node, string name)
    {
        if (node.Name == name)
        {
            return node;
        }

        if (node is GroupNodeResponse group)
        {
            foreach (var child in group.Children)
            {
                var match = TryFindNode(child, name);
                if (match != null)
                {
                    return match;
                }
            }
        }

        return null;
    }

    private static ProgramSimulationNodeResponse FindNode(ProgramSimulationNodeResponse node, string name)
    {
        if (node.Name == name)
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var match = TryFindNode(child, name);
            if (match != null)
            {
                return match;
            }
        }

        throw new InvalidOperationException($"Simulation node '{name}' was not found.");
    }

    private static ProgramSimulationNodeResponse? TryFindNode(ProgramSimulationNodeResponse node, string name)
    {
        if (node.Name == name)
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var match = TryFindNode(child, name);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void AssertNotChosen(ProgramSimulationNodeResponse root, string nodeName)
    {
        var node = FindNode(root, nodeName);
        Assert.Equal(SimulationStatus.Blocked, node.Status);
        Assert.Equal("Not part of the chosen path.", node.BlockedReason);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var value = JsonSerializer.Deserialize<T>(content, JsonOptions);
        Assert.NotNull(value);
        return value;
    }
}

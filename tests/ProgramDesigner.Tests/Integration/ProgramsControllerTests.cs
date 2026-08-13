namespace ProgramDesigner.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using ProgramDesigner.Api.Dto;
using ProgramDesigner.Core.Domain;
using Xunit;

public class ProgramsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProgramsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    [Fact]
    public async Task PostProgram_ValidComputerScienceScenario_Returns201Created()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var request = new ProgramCreateRequest
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
                                GroupRule = GroupRule.Choice,
                                PickCount = 2,
                                Children =
                                [
                                    new StepNodeRequest { Name = "Machine Learning", StepType = "session" },
                                    new StepNodeRequest { Name = "Neural Networks", StepType = "session" },
                                    new StepNodeRequest { Name = "Computer Vision", StepType = "session" }
                                ]
                            },
                            new GroupNodeRequest { Name = "IT", GroupRule = GroupRule.InOrder, Children = [new StepNodeRequest { Name = "A", StepType = "test" }] },
                            new GroupNodeRequest { Name = "Programming", GroupRule = GroupRule.InOrder, Children = [new StepNodeRequest { Name = "B", StepType = "test" }] }
                        ]
                    },
                    new StepNodeRequest
                    {
                        Name = "Final Capstone",
                        StepType = "submission"
                    }
                ]
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/programs", request, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var programResponse = JsonSerializer.Deserialize<ProgramResponse>(content, _jsonOptions);

        Assert.NotNull(programResponse);
        Assert.NotEqual(Guid.Empty, programResponse.Id);
        Assert.Equal("Computer Science", programResponse.Name);
        Assert.Equal(3, programResponse.RootGroup.Children.Count);
        
        var foundations = Assert.IsType<GroupNodeResponse>(programResponse.RootGroup.Children[0]);
        var major = Assert.IsType<GroupNodeResponse>(programResponse.RootGroup.Children[1]);
        
        // Ensure prerequisite was resolved to real Guid
        Assert.NotNull(major.PrerequisiteId);
        Assert.Equal(foundations.Id, major.PrerequisiteId);
    }

    [Fact]
    public async Task GetProgram_ValidId_Returns200WithCorrectTree()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var request = new ProgramCreateRequest
        {
            Name = "Get Test Program",
            RootGroup = new GroupNodeRequest
            {
                Name = "Root",
                GroupRule = GroupRule.InOrder,
                Children =
                [
                    new StepNodeRequest { Key = "step1", Name = "Step 1", StepType = "session" },
                    new StepNodeRequest { Name = "Step 2", StepType = "test", PrerequisiteRef = "step1" }
                ]
            }
        };

        var postResponse = await client.PostAsJsonAsync("/programs", request, _jsonOptions);
        postResponse.EnsureSuccessStatusCode();
        var content = await postResponse.Content.ReadAsStringAsync();
        var createdProgram = JsonSerializer.Deserialize<ProgramResponse>(content, _jsonOptions);
        Assert.NotNull(createdProgram);

        // Act
        var getResponse = await client.GetAsync($"/programs/{createdProgram.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var fetchedProgram = JsonSerializer.Deserialize<ProgramResponse>(getContent, _jsonOptions);
        
        Assert.NotNull(fetchedProgram);
        Assert.Equal(createdProgram.Id, fetchedProgram.Id);
        Assert.Equal("Get Test Program", fetchedProgram.Name);
        Assert.Equal(2, fetchedProgram.RootGroup.Children.Count);
        
        var step1 = Assert.IsType<StepNodeResponse>(fetchedProgram.RootGroup.Children[0]);
        var step2 = Assert.IsType<StepNodeResponse>(fetchedProgram.RootGroup.Children[1]);
        
        Assert.Equal("Step 2", step2.Name);
        Assert.Equal(step1.Id, step2.PrerequisiteId);
        Assert.Equal("Step 1", step2.PrerequisiteName);
    }

    [Fact]
    public async Task GetProgram_InvalidId_Returns404NotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var randomId = Guid.NewGuid();

        // Act
        var getResponse = await client.GetAsync($"/programs/{randomId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        var content = await getResponse.Content.ReadAsStringAsync();
        Assert.Contains($"Program {randomId} not found", content);
    }

    [Fact]
    public async Task ValidateProgram_InvalidId_Returns404NotFound()
    {
        var client = _factory.CreateClient();
        var randomId = Guid.NewGuid();

        var validateResponse = await client.PostAsync($"/programs/{randomId}/validate", null);

        Assert.Equal(HttpStatusCode.NotFound, validateResponse.StatusCode);
    }
}

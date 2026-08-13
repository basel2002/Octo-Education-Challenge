namespace ProgramDesigner.Application.Services;

using ProgramDesigner.Application.Dto;
using ProgramDesigner.Application.Mapping;
using ProgramDesigner.Core.Domain;
using ProgramDesigner.Core.Repositories;
using ProgramDesigner.Core.Services;
using System.Linq;

public sealed class SimulateProgramAppService
{
    private readonly IEducationProgramRepository _repository;
    private readonly ProgramSimulationService _simulationService;
    private readonly ProgramMapper _mapper;

    public SimulateProgramAppService(
        IEducationProgramRepository repository, 
        ProgramSimulationService simulationService, 
        ProgramMapper mapper)
    {
        _repository = repository;
        _simulationService = simulationService;
        _mapper = mapper;
    }

    public async Task<(ProgramSimulationResponse? Response, List<string> Errors)> ExecuteAsync(Guid id, ProgramSimulationRequest request)
    {
        var program = await _repository.GetAsync(id);

        if (program == null)
        {
            return (null, new List<string> { $"Program {id} not found" });
        }

        var choices = request.Choices.ToDictionary(
            choice => new NodeId(choice.Key),
            choice => (IReadOnlyCollection<NodeId>)choice.Value.Select(v => new NodeId(v)).ToList());
            
        var completedStepIds = request.CompletedStepIds.Select(v => new NodeId(v)).ToHashSet();

        var (rootNode, errors) = _simulationService.Simulate(program, choices, completedStepIds);
        
        if (errors.Any() || rootNode == null)
        {
            return (null, errors);
        }

        return (_mapper.MapToSimulationResponse(rootNode), errors);
    }
}

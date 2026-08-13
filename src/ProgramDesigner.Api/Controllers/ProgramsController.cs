namespace ProgramDesigner.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using ProgramDesigner.Api.Dto;
using ProgramDesigner.Api.Mapping;
using ProgramDesigner.Core.Repositories;
using ProgramDesigner.Core.Services;

[ApiController]
[Route("programs")]
public class ProgramsController : ControllerBase
{
    private readonly IEducationProgramRepository _repository;
    private readonly ProgramMapper _mapper;
    private readonly ProgramValidationService _validationService;
    private readonly ProgramSimulationService _simulationService;

    public ProgramsController(
        IEducationProgramRepository repository,
        ProgramMapper mapper,
        ProgramValidationService validationService,
        ProgramSimulationService simulationService)
    {
        _repository = repository;
        _mapper = mapper;
        _validationService = validationService;
        _simulationService = simulationService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProgram([FromBody] ProgramCreateRequest request)
    {
        var (program, errors) = _mapper.MapToDomain(request);

        if (errors.Any() || program == null)
        {
            return BadRequest(new { Errors = errors });
        }

        await _repository.AddAsync(program);

        var responseDto = _mapper.MapToResponse(program);

        return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, responseDto);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProgram(Guid id)
    {
        var program = await _repository.GetAsync(id);

        if (program == null)
        {
            return NotFound(new { Error = $"Program {id} not found" });
        }

        var responseDto = _mapper.MapToResponse(program);

        return Ok(responseDto);
    }

    [HttpPost("{id}/validate")]
    public async Task<IActionResult> ValidateProgram(Guid id)
    {
        var program = await _repository.GetAsync(id);

        if (program == null)
        {
            return NotFound(new { Error = $"Program {id} not found" });
        }

        var (isValid, impossiblePrereqs, reachabilityWarnings) = _validationService.Validate(program);

        var result = _mapper.MapToValidationResultResponse(isValid, impossiblePrereqs, reachabilityWarnings);

        return Ok(result);
    }

    [HttpPost("{id}/simulate")]
    public async Task<IActionResult> SimulateProgram(Guid id, [FromBody] ProgramSimulationRequest request)
    {
        var program = await _repository.GetAsync(id);

        if (program == null)
        {
            return NotFound(new { Error = $"Program {id} not found" });
        }

        var choices = request.Choices.ToDictionary(
            choice => choice.Key,
            choice => (IReadOnlyCollection<Guid>)choice.Value);
        var completedStepIds = request.CompletedStepIds.ToHashSet();

        var (rootNode, errors) = _simulationService.Simulate(program, choices, completedStepIds);
        if (errors.Any() || rootNode == null)
        {
            return BadRequest(new { Errors = errors });
        }

        return Ok(_mapper.MapToSimulationResponse(rootNode));
    }
}

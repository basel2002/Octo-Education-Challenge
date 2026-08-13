namespace ProgramDesigner.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using ProgramDesigner.Application.Dto;
using ProgramDesigner.Application.Services;

[ApiController]
[Route("programs")]
public class ProgramsController : ControllerBase
{
    private readonly CreateProgramService _createService;
    private readonly GetProgramService _getService;
    private readonly ValidateProgramService _validateService;
    private readonly SimulateProgramAppService _simulateService;

    public ProgramsController(
        CreateProgramService createService,
        GetProgramService getService,
        ValidateProgramService validateService,
        SimulateProgramAppService simulateService)
    {
        _createService = createService;
        _getService = getService;
        _validateService = validateService;
        _simulateService = simulateService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProgram([FromBody] ProgramCreateRequest request)
    {
        var (responseDto, errors) = await _createService.ExecuteAsync(request);

        if (errors.Any() || responseDto == null)
        {
            return BadRequest(new { Errors = errors });
        }

        return CreatedAtAction(nameof(GetProgram), new { id = responseDto.Id }, responseDto);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProgram(Guid id)
    {
        var responseDto = await _getService.ExecuteAsync(id);

        if (responseDto == null)
        {
            return NotFound(new { Error = $"Program {id} not found" });
        }

        return Ok(responseDto);
    }

    [HttpPost("{id}/validate")]
    public async Task<IActionResult> ValidateProgram(Guid id)
    {
        var result = await _validateService.ExecuteAsync(id);

        if (result == null)
        {
            return NotFound(new { Error = $"Program {id} not found" });
        }

        return Ok(result);
    }

    [HttpPost("{id}/simulate")]
    public async Task<IActionResult> SimulateProgram(Guid id, [FromBody] ProgramSimulationRequest request)
    {
        var (responseDto, errors) = await _simulateService.ExecuteAsync(id, request);
        
        if (errors.Any())
        {
            if (responseDto == null && errors.Count == 1 && errors[0].Contains("not found"))
            {
                return NotFound(new { Error = errors[0] });
            }
            return BadRequest(new { Errors = errors });
        }

        return Ok(responseDto);
    }
}

namespace ProgramDesigner.Application.Services;

using ProgramDesigner.Application.Dto;
using ProgramDesigner.Application.Mapping;
using ProgramDesigner.Core.Repositories;
using ProgramDesigner.Core.Services;

public sealed class ValidateProgramService
{
    private readonly IEducationProgramRepository _repository;
    private readonly ProgramValidationService _validationService;
    private readonly ProgramMapper _mapper;

    public ValidateProgramService(
        IEducationProgramRepository repository, 
        ProgramValidationService validationService, 
        ProgramMapper mapper)
    {
        _repository = repository;
        _validationService = validationService;
        _mapper = mapper;
    }

    public async Task<ValidationResultResponse?> ExecuteAsync(Guid id)
    {
        var program = await _repository.GetAsync(id);

        if (program == null)
        {
            return null;
        }

        var (isValid, impossiblePrereqs, reachabilityWarnings) = _validationService.Validate(program);

        return _mapper.MapToValidationResultResponse(isValid, impossiblePrereqs, reachabilityWarnings);
    }
}

namespace ProgramDesigner.Application.Services;

using ProgramDesigner.Application.Dto;
using ProgramDesigner.Application.Mapping;
using ProgramDesigner.Core.Repositories;

public sealed class CreateProgramService
{
    private readonly IEducationProgramRepository _repository;
    private readonly ProgramMapper _mapper;

    public CreateProgramService(IEducationProgramRepository repository, ProgramMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<(ProgramResponse? Response, List<string> Errors)> ExecuteAsync(ProgramCreateRequest request)
    {
        var (program, errors) = _mapper.MapToDomain(request);

        if (errors.Any() || program == null)
        {
            return (null, errors);
        }

        await _repository.AddAsync(program);
        
        var responseDto = _mapper.MapToResponse(program);
        return (responseDto, errors);
    }
}

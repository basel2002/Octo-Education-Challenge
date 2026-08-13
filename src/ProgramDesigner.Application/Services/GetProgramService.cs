namespace ProgramDesigner.Application.Services;

using ProgramDesigner.Application.Dto;
using ProgramDesigner.Application.Mapping;
using ProgramDesigner.Core.Repositories;

public sealed class GetProgramService
{
    private readonly IEducationProgramRepository _repository;
    private readonly ProgramMapper _mapper;

    public GetProgramService(IEducationProgramRepository repository, ProgramMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ProgramResponse?> ExecuteAsync(Guid id)
    {
        var program = await _repository.GetAsync(id);

        if (program == null)
        {
            return null;
        }

        return _mapper.MapToResponse(program);
    }
}

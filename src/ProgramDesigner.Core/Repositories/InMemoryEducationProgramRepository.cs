namespace ProgramDesigner.Core.Repositories;

using System.Collections.Concurrent;
using ProgramDesigner.Core.Domain;

/// <summary>
/// A simple thread-safe, in-memory implementation of <see cref="IEducationProgramRepository"/>.
/// </summary>
public sealed class InMemoryEducationProgramRepository : IEducationProgramRepository
{
    private readonly ConcurrentDictionary<Guid, EducationProgram> _store = new();

    public Task AddAsync(EducationProgram program)
    {
        _store.TryAdd(program.Id, program);
        return Task.CompletedTask;
    }

    public Task<EducationProgram?> GetAsync(Guid id)
    {
        _store.TryGetValue(id, out var program);
        return Task.FromResult(program);
    }
}

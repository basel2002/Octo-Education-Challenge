namespace ProgramDesigner.Core.Repositories;

using ProgramDesigner.Core.Domain;

/// <summary>
/// Repository interface for storing and retrieving <see cref="EducationProgram"/> aggregates.
/// </summary>
public interface IEducationProgramRepository
{
    /// <summary>
    /// Adds a new <see cref="EducationProgram"/> to the repository.
    /// </summary>
    Task AddAsync(EducationProgram program);

    /// <summary>
    /// Retrieves an <see cref="EducationProgram"/> by its ID, or null if not found.
    /// </summary>
    Task<EducationProgram?> GetAsync(Guid id);
}

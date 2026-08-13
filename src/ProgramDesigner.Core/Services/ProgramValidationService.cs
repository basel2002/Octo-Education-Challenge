namespace ProgramDesigner.Core.Services;

using System.Collections.Generic;
using System.Linq;
using ProgramDesigner.Core.Domain;
using ProgramDesigner.Core.Validators;

public class ProgramValidationService
{
    private readonly PrerequisiteValidator _prerequisiteValidator;
    private readonly ReachabilityValidator _reachabilityValidator;

    public ProgramValidationService(PrerequisiteValidator prerequisiteValidator, ReachabilityValidator reachabilityValidator)
    {
        _prerequisiteValidator = prerequisiteValidator;
        _reachabilityValidator = reachabilityValidator;
    }

    public (bool IsValid, IReadOnlyList<ImpossiblePrerequisite> ImpossiblePrerequisites, IReadOnlyList<ReachabilityWarning> ReachabilityWarnings) Validate(EducationProgram program)
    {
        var impossiblePrereqs = _prerequisiteValidator.FindImpossiblePrerequisites(program);
        var reachabilityWarnings = _reachabilityValidator.FindReachabilityWarnings(program);

        var isValid = !impossiblePrereqs.Any();

        return (isValid, impossiblePrereqs, reachabilityWarnings);
    }
}

namespace ProgramDesigner.Api.Mapping;

using ProgramDesigner.Api.Dto;
using ProgramDesigner.Core.Domain;

public sealed class ProgramMapper
{
    public (EducationProgram? Program, List<string> Errors) MapToDomain(ProgramCreateRequest request)
    {
        var errors = new List<string>();
        var keyToGuidMap = new Dictionary<string, Guid>();
        
        var programId = Guid.NewGuid();

        // Pass 1: Pre-collect keys and assign Guids
        CollectKeys(request.RootGroup, keyToGuidMap, errors);
        
        if (errors.Any())
        {
            return (null, errors);
        }

        // Pass 2: Build tree and assign Guids and prerequisites
        var rootGroup = MapNodeRequest(request.RootGroup, keyToGuidMap, errors);
        
        if (errors.Any() || rootGroup is not GroupNode mappedRootGroup)
        {
            return (null, errors);
        }

        var program = new EducationProgram
        {
            Id = programId,
            Name = request.Name,
            RootGroup = mappedRootGroup
        };
        
        try
        {
            // Validate basic invariants for the tree (Choice PickCount etc)
            program.ValidateInvariants();
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }

        if (errors.Any())
        {
            return (null, errors);
        }

        return (program, errors);
    }

    private void CollectKeys(ProgramNodeRequest node, Dictionary<string, Guid> keyToGuidMap, List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(node.Key))
        {
            if (!keyToGuidMap.TryAdd(node.Key, Guid.NewGuid()))
            {
                errors.Add($"Duplicate key found: '{node.Key}'. Keys must be unique across the entire program.");
            }
        }

        if (node is GroupNodeRequest groupReq)
        {
            foreach (var child in groupReq.Children)
            {
                CollectKeys(child, keyToGuidMap, errors);
            }
        }
    }

    private ProgramNode? MapNodeRequest(ProgramNodeRequest request, Dictionary<string, Guid> keyToGuidMap, List<string> errors)
    {
        // If it had a key, we already assigned an ID. Otherwise generate a new one.
        var id = (!string.IsNullOrWhiteSpace(request.Key) && keyToGuidMap.TryGetValue(request.Key, out var existingId)) 
            ? existingId 
            : Guid.NewGuid();

        Guid? prereqId = null;
        if (!string.IsNullOrWhiteSpace(request.PrerequisiteRef))
        {
            if (keyToGuidMap.TryGetValue(request.PrerequisiteRef, out var resolvedId))
            {
                prereqId = resolvedId;
            }
            else
            {
                errors.Add($"Unresolvable prerequisiteRef: '{request.PrerequisiteRef}' on node '{request.Name}'.");
            }
        }

        if (request is StepNodeRequest stepReq)
        {
            return new StepNode
            {
                Id = id,
                Name = stepReq.Name,
                PrerequisiteId = prereqId,
                StepType = stepReq.StepType
            };
        }
        else if (request is GroupNodeRequest groupReq)
        {
            var children = new List<ProgramNode>();
            foreach (var childReq in groupReq.Children)
            {
                var mappedChild = MapNodeRequest(childReq, keyToGuidMap, errors);
                if (mappedChild != null)
                {
                    children.Add(mappedChild);
                }
            }

            return new GroupNode
            {
                Id = id,
                Name = groupReq.Name,
                PrerequisiteId = prereqId,
                GroupRule = groupReq.GroupRule,
                PickCount = groupReq.PickCount,
                Children = children
            };
        }

        errors.Add($"Unknown node type for node '{request.Name}'.");
        return null;
    }

    public ProgramResponse MapToResponse(EducationProgram program)
    {
        var idToNameMap = new Dictionary<Guid, string>();
        CollectNames(program.RootGroup, idToNameMap);

        return new ProgramResponse
        {
            Id = program.Id,
            Name = program.Name,
            RootGroup = (GroupNodeResponse)MapNodeResponse(program.RootGroup, idToNameMap)
        };
    }

    private void CollectNames(ProgramNode node, Dictionary<Guid, string> idToNameMap)
    {
        idToNameMap[node.Id] = node.Name;
        
        if (node is GroupNode groupNode)
        {
            foreach (var child in groupNode.Children)
            {
                CollectNames(child, idToNameMap);
            }
        }
    }

    private ProgramNodeResponse MapNodeResponse(ProgramNode node, Dictionary<Guid, string> idToNameMap)
    {
        string? prereqName = null;
        if (node.PrerequisiteId.HasValue)
        {
            idToNameMap.TryGetValue(node.PrerequisiteId.Value, out prereqName);
        }

        if (node is StepNode stepNode)
        {
            return new StepNodeResponse
            {
                Id = stepNode.Id,
                Name = stepNode.Name,
                PrerequisiteId = stepNode.PrerequisiteId,
                PrerequisiteName = prereqName,
                StepType = stepNode.StepType
            };
        }
        else if (node is GroupNode groupNode)
        {
            return new GroupNodeResponse
            {
                Id = groupNode.Id,
                Name = groupNode.Name,
                PrerequisiteId = groupNode.PrerequisiteId,
                PrerequisiteName = prereqName,
                GroupRule = groupNode.GroupRule,
                PickCount = groupNode.PickCount,
                Children = groupNode.Children.Select(c => MapNodeResponse(c, idToNameMap)).ToList()
            };
        }
        
        throw new InvalidOperationException($"Unknown node type: {node.GetType().Name}");
    }

    public ValidationResultResponse MapToValidationResultResponse(
        bool isValid, 
        IReadOnlyList<ImpossiblePrerequisite> impossiblePrereqs, 
        IReadOnlyList<ReachabilityWarning> reachabilityWarnings)
    {
        return new ValidationResultResponse
        {
            IsValid = isValid,
            ImpossiblePrerequisites = impossiblePrereqs.Select(ip => new ImpossiblePrerequisiteResponse
            {
                NodeId = ip.NodeId,
                NodeName = ip.NodeName,
                PrerequisiteId = ip.PrerequisiteId,
                PrerequisiteName = ip.PrerequisiteName,
                Reason = ip.Reason,
                Description = ip.Description
            }).ToList(),
            ReachabilityWarnings = reachabilityWarnings.Select(rw => new ReachabilityWarningResponse
            {
                NodeId = rw.NodeId,
                NodeName = rw.NodeName,
                PrerequisiteId = rw.PrerequisiteId,
                PrerequisiteName = rw.PrerequisiteName,
                RiskyChoiceGroupId = rw.RiskyChoiceGroupId,
                RiskyChoiceGroupName = rw.RiskyChoiceGroupName,
                Description = rw.Description
            }).ToList()
        };
    }

    public ProgramSimulationResponse MapToSimulationResponse(ProgramSimulationNode rootNode)
    {
        return new ProgramSimulationResponse
        {
            RootNode = MapSimulationNodeResponse(rootNode)
        };
    }

    private ProgramSimulationNodeResponse MapSimulationNodeResponse(ProgramSimulationNode node)
    {
        return new ProgramSimulationNodeResponse
        {
            Id = node.Id,
            Name = node.Name,
            NodeType = node.NodeType,
            Status = node.Status,
            BlockedReason = node.BlockedReason,
            Children = node.Children.Select(MapSimulationNodeResponse).ToList()
        };
    }
}

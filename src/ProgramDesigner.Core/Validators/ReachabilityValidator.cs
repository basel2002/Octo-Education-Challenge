namespace ProgramDesigner.Core.Validators;

using System.Collections.Generic;
using System.Linq;
using ProgramDesigner.Core.Domain;

public class ReachabilityValidator
{
    public IReadOnlyList<ReachabilityWarning> FindReachabilityWarnings(EducationProgram program)
    {
        var nodeMap = new Dictionary<Guid, ProgramNode>();
        var parentMap = new Dictionary<Guid, GroupNode>();

        // Pass 1: Build maps
        BuildMaps(program.RootGroup, null, nodeMap, parentMap);

        var warnings = new List<ReachabilityWarning>();

        // Pass 2: Check prerequisites for reachability risks
        foreach (var sourceNode in nodeMap.Values.Where(n => n.PrerequisiteId.HasValue))
        {
            var targetId = sourceNode.PrerequisiteId!.Value;
            
            if (!nodeMap.TryGetValue(targetId, out var targetNode))
            {
                continue; // Impossible or invalid prerequisite, handled by other validators
            }

            var currentParent = parentMap.TryGetValue(targetNode.Id, out var p) ? p : null;
            var pathBranch = targetNode;
            
            GroupNode? outermostRiskyChoiceGroup = null;

            // Walk up from the prerequisite target to the root
            while (currentParent != null)
            {
                if (currentParent.GroupRule == GroupRule.Choice)
                {
                    // LCA-exclusion check: If the source node is inside the SAME branch of the Choice group
                    // as the target node, then the risk is negated (if the user skips the branch, they skip both).
                    bool sourceSharesBranch = IsDescendantOrSelf(sourceNode, pathBranch, parentMap);
                    
                    if (!sourceSharesBranch)
                    {
                        // Record the outermost risky Choice group. We choose outermost because it represents 
                        // the highest-level architectural decision where the participant could bypass the target.
                        outermostRiskyChoiceGroup = currentParent;
                    }
                }

                pathBranch = currentParent;
                currentParent = parentMap.TryGetValue(currentParent.Id, out var gp) ? gp : null;
            }

            if (outermostRiskyChoiceGroup != null)
            {
                warnings.Add(new ReachabilityWarning
                {
                    NodeId = sourceNode.Id,
                    NodeName = sourceNode.Name,
                    PrerequisiteId = targetNode.Id,
                    PrerequisiteName = targetNode.Name,
                    RiskyChoiceGroupId = outermostRiskyChoiceGroup.Id,
                    RiskyChoiceGroupName = outermostRiskyChoiceGroup.Name,
                    Description = $"The prerequisite on '{targetNode.Name}' is only guaranteed if the participant picks the specific option under the '{outermostRiskyChoiceGroup.Name}' choice group. Participants who choose other options can never satisfy it."
                });
            }
        }

        return warnings;
    }

    private void BuildMaps(ProgramNode node, GroupNode? parent, Dictionary<Guid, ProgramNode> nodeMap, Dictionary<Guid, GroupNode> parentMap)
    {
        nodeMap[node.Id] = node;
        if (parent != null)
        {
            parentMap[node.Id] = parent;
        }

        if (node is GroupNode group)
        {
            foreach (var child in group.Children)
            {
                BuildMaps(child, group, nodeMap, parentMap);
            }
        }
    }

    private bool IsDescendantOrSelf(ProgramNode node, ProgramNode possibleAncestor, Dictionary<Guid, GroupNode> parentMap)
    {
        var current = node;
        while (current != null)
        {
            if (current.Id == possibleAncestor.Id)
            {
                return true;
            }
            
            current = parentMap.TryGetValue(current.Id, out var p) ? p : null;
        }
        
        return false;
    }
}

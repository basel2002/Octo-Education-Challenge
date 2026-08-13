namespace ProgramDesigner.Core.Validators;

using System.Collections.Generic;
using ProgramDesigner.Core.Domain;

public class PrerequisiteValidator
{
    public IReadOnlyList<ImpossiblePrerequisite> FindImpossiblePrerequisites(EducationProgram program)
    {
        var descendantsMap = new Dictionary<NodeId, HashSet<NodeId>>();
        var preOrderIndices = new Dictionary<NodeId, int>();
        var names = new Dictionary<NodeId, string>();
        
        var currentIndex = 0;
        
        // Pass 1: Build maps
        BuildMaps(program.RootGroup, ref currentIndex, descendantsMap, preOrderIndices, names);

        var results = new List<ImpossiblePrerequisite>();

        // Pass 2: Check prerequisites
        CheckPrerequisites(program.RootGroup, descendantsMap, preOrderIndices, names, results);

        return results;
    }

    private HashSet<NodeId> BuildMaps(
        ProgramNode node, 
        ref int currentIndex, 
        Dictionary<NodeId, HashSet<NodeId>> descendantsMap, 
        Dictionary<NodeId, int> preOrderIndices, 
        Dictionary<NodeId, string> names)
    {
        var thisIndex = currentIndex++;
        preOrderIndices[node.Id] = thisIndex;
        names[node.Id] = node.Name;
        
        var descendants = new HashSet<NodeId>();
        
        if (node is GroupNode group)
        {
            foreach (var child in group.Children)
            {
                var childDescendants = BuildMaps(child, ref currentIndex, descendantsMap, preOrderIndices, names);
                descendants.Add(child.Id);
                descendants.UnionWith(childDescendants);
            }
        }
        
        descendantsMap[node.Id] = descendants;
        return descendants;
    }

    private void CheckPrerequisites(
        ProgramNode node, 
        Dictionary<NodeId, HashSet<NodeId>> descendantsMap, 
        Dictionary<NodeId, int> preOrderIndices, 
        Dictionary<NodeId, string> names, 
        List<ImpossiblePrerequisite> results)
    {
        if (node.PrerequisiteId.HasValue)
        {
            var prereqId = node.PrerequisiteId.Value;
            
            // Only validate if the prerequisite actually exists in this tree.
            // (If it doesn't exist, it's a different kind of error, but here we assume it exists
            // because mapping logic catches unresolvable keys, though a raw Guid could be invalid).
            if (preOrderIndices.TryGetValue(prereqId, out var prereqIndex))
            {
                var prereqName = names[prereqId];
                var thisIndex = preOrderIndices[node.Id];

                if (prereqId == node.Id)
                {
                    results.Add(new ImpossiblePrerequisite
                    {
                        NodeId = node.Id,
                        NodeName = node.Name,
                        PrerequisiteId = prereqId,
                        PrerequisiteName = prereqName,
                        Reason = ImpossiblePrerequisiteReason.SelfReference,
                        Description = $"Node '{node.Name}' cannot depend on itself."
                    });
                }
                else if (descendantsMap[node.Id].Contains(prereqId))
                {
                    results.Add(new ImpossiblePrerequisite
                    {
                        NodeId = node.Id,
                        NodeName = node.Name,
                        PrerequisiteId = prereqId,
                        PrerequisiteName = prereqName,
                        Reason = ImpossiblePrerequisiteReason.DescendantReference,
                        Description = $"Node '{node.Name}' cannot depend on '{prereqName}' because '{prereqName}' is inside its own subtree."
                    });
                }
                else if (prereqIndex >= thisIndex)
                {
                    results.Add(new ImpossiblePrerequisite
                    {
                        NodeId = node.Id,
                        NodeName = node.Name,
                        PrerequisiteId = prereqId,
                        PrerequisiteName = prereqName,
                        Reason = ImpossiblePrerequisiteReason.ForwardReference,
                        Description = $"Node '{node.Name}' cannot depend on '{prereqName}' because '{prereqName}' appears later or alongside it in the required completion order."
                    });
                }
            }
        }

        if (node is GroupNode group)
        {
            foreach (var child in group.Children)
            {
                CheckPrerequisites(child, descendantsMap, preOrderIndices, names, results);
            }
        }
    }
}

namespace ProgramDesigner.Core.Services;

using ProgramDesigner.Core.Domain;

public sealed class ProgramSimulationService
{
    public (ProgramSimulationNode? RootNode, List<string> Errors) Simulate(
        EducationProgram program,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> choices,
        IReadOnlySet<Guid> completedStepIds)
    {
        var errors = ValidateChoices(program, choices);
        if (errors.Count > 0)
        {
            return (null, errors);
        }

        var nodeMap = BuildNodeMap(program.RootGroup);
        var completionMap = new Dictionary<Guid, bool>();
        ComputeCompletion(program.RootGroup, choices, completedStepIds, completionMap);

        var root = BuildSimulationNode(
            program.RootGroup,
            parent: null,
            previousSibling: null,
            choices,
            nodeMap,
            completionMap);

        return (root, errors);
    }

    private static List<string> ValidateChoices(
        EducationProgram program,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> choices)
    {
        var errors = new List<string>();
        var nodeMap = BuildNodeMap(program.RootGroup);

        foreach (var (choiceGroupId, pickedChildIds) in choices)
        {
            if (!nodeMap.TryGetValue(choiceGroupId, out var choiceNode))
            {
                errors.Add($"Choice group id '{choiceGroupId}' does not exist in this program.");
                continue;
            }

            if (choiceNode is not GroupNode choiceGroup || choiceGroup.GroupRule != GroupRule.Choice)
            {
                errors.Add($"Node '{choiceNode.Name}' ({choiceGroupId}) is not a Choice group.");
                continue;
            }

            var validChildIds = choiceGroup.Children.Select(c => c.Id).ToHashSet();
            foreach (var pickedChildId in pickedChildIds)
            {
                if (!validChildIds.Contains(pickedChildId))
                {
                    errors.Add($"Choice group '{choiceGroup.Name}' does not contain child id '{pickedChildId}'.");
                }
            }
        }

        return errors;
    }

    private static Dictionary<Guid, ProgramNode> BuildNodeMap(ProgramNode root)
    {
        var nodeMap = new Dictionary<Guid, ProgramNode>();
        AddNode(root, nodeMap);
        return nodeMap;
    }

    private static void AddNode(ProgramNode node, Dictionary<Guid, ProgramNode> nodeMap)
    {
        nodeMap[node.Id] = node;

        if (node is GroupNode group)
        {
            foreach (var child in group.Children)
            {
                AddNode(child, nodeMap);
            }
        }
    }

    private static bool ComputeCompletion(
        ProgramNode node,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> choices,
        IReadOnlySet<Guid> completedStepIds,
        Dictionary<Guid, bool> completionMap)
    {
        bool isComplete;
        if (node is StepNode)
        {
            isComplete = completedStepIds.Contains(node.Id);
        }
        else
        {
            var group = (GroupNode)node;
            var childCompletion = group.Children
                .Select(child => ComputeCompletion(child, choices, completedStepIds, completionMap))
                .ToList();

            isComplete = group.GroupRule == GroupRule.InOrder
                ? childCompletion.All(completed => completed)
                : CountCompleteChosenChildren(group, choices, completionMap) >= group.PickCount!.Value;
        }

        completionMap[node.Id] = isComplete;
        return isComplete;
    }

    private static int CountCompleteChosenChildren(
        GroupNode choiceGroup,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> choices,
        Dictionary<Guid, bool> completionMap)
    {
        if (!choices.TryGetValue(choiceGroup.Id, out var pickedChildIds))
        {
            return 0;
        }

        var pickedSet = pickedChildIds.ToHashSet();
        return choiceGroup.Children
            .Where(child => pickedSet.Contains(child.Id))
            .Count(child => completionMap[child.Id]);
    }

    private static ProgramSimulationNode BuildSimulationNode(
        ProgramNode node,
        GroupNode? parent,
        ProgramNode? previousSibling,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> choices,
        IReadOnlyDictionary<Guid, ProgramNode> nodeMap,
        IReadOnlyDictionary<Guid, bool> completionMap)
    {
        var status = GetStatus(node, parent, previousSibling, choices, nodeMap, completionMap, out var blockedReason);
        var children = new List<ProgramSimulationNode>();

        if (node is GroupNode group)
        {
            ProgramNode? prior = null;
            foreach (var child in group.Children)
            {
                children.Add(BuildSimulationNode(child, group, prior, choices, nodeMap, completionMap));
                prior = child;
            }
        }

        return new ProgramSimulationNode
        {
            Id = node.Id,
            Name = node.Name,
            NodeType = node.NodeType,
            Status = status,
            BlockedReason = blockedReason,
            Children = children
        };
    }

    private static SimulationStatus GetStatus(
        ProgramNode node,
        GroupNode? parent,
        ProgramNode? previousSibling,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> choices,
        IReadOnlyDictionary<Guid, ProgramNode> nodeMap,
        IReadOnlyDictionary<Guid, bool> completionMap,
        out string? blockedReason)
    {
        blockedReason = null;

        if (completionMap[node.Id])
        {
            return SimulationStatus.Complete;
        }

        if (parent is { GroupRule: GroupRule.Choice }
            && choices.TryGetValue(parent.Id, out var pickedChildIds)
            && !pickedChildIds.Contains(node.Id))
        {
            blockedReason = "Not part of the chosen path.";
            return SimulationStatus.Blocked;
        }

        if (node.PrerequisiteId.HasValue
            && nodeMap.TryGetValue(node.PrerequisiteId.Value, out var prerequisite)
            && !completionMap[prerequisite.Id])
        {
            blockedReason = $"Blocked: prerequisite '{prerequisite.Name}' not yet complete.";
            return SimulationStatus.Blocked;
        }

        if (parent is { GroupRule: GroupRule.InOrder }
            && previousSibling is not null
            && !completionMap[previousSibling.Id])
        {
            blockedReason = $"Blocked: previous node '{previousSibling.Name}' not yet complete.";
            return SimulationStatus.Blocked;
        }

        return SimulationStatus.Unlocked;
    }
}

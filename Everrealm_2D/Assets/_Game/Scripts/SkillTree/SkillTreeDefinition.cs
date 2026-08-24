using System;
using System.Collections.Generic;
using UnityEngine;

namespace LetterHunter.SkillTree
{
    [CreateAssetMenu(menuName = "Letter Hunter/Skill Tree/Skill Tree Definition")]
    public sealed class SkillTreeDefinition : ScriptableObject
    {
        [SerializeField] private string treeId = "warrior_tree";
        [SerializeField] private string displayName = "Skill Tree";
        [SerializeField] private SkillTreeNodeDefinition[] nodes = Array.Empty<SkillTreeNodeDefinition>();
        [Header("Respec")]
        [Range(0f, 1f), SerializeField] private float goldRefundRate = 0.75f;
        [SerializeField] private bool refundMaterials;

        public string TreeId => string.IsNullOrWhiteSpace(treeId) ? name : treeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public IReadOnlyList<SkillTreeNodeDefinition> Nodes => nodes;
        public float GoldRefundRate => Mathf.Clamp01(goldRefundRate);
        public bool RefundMaterials => refundMaterials;

        public bool TryGetNode(string nodeId, out SkillTreeNodeDefinition node)
        {
            node = null;
            if (string.IsNullOrWhiteSpace(nodeId) || nodes == null)
                return false;

            foreach (var candidate in nodes)
            {
                if (candidate != null && candidate.NodeId == nodeId)
                {
                    node = candidate;
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<string> ValidateDefinition()
        {
            var errors = new List<string>();
            var byId = new Dictionary<string, SkillTreeNodeDefinition>();
            foreach (var node in nodes ?? Array.Empty<SkillTreeNodeDefinition>())
            {
                if (node == null)
                {
                    errors.Add("Tree contains a null node.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.NodeId))
                {
                    errors.Add("Tree contains a node without an id.");
                    continue;
                }

                if (!byId.TryAdd(node.NodeId, node))
                    errors.Add($"Duplicate node id: {node.NodeId}.");
                if (node.UnlockAction == SkillTreeUnlockAction.UnlockSkill && node.SkillToUnlock == null)
                    errors.Add($"Node {node.NodeId} unlocks a skill but has no SkillDefinition.");

                foreach (var effect in node.RankEffects)
                {
                    if (effect == null)
                    {
                        errors.Add($"Node {node.NodeId} contains a null rank effect.");
                        continue;
                    }
                    if (effect.FirstAppliedRank > node.MaxRank)
                        errors.Add($"Node {node.NodeId} effect starts at rank {effect.FirstAppliedRank}, above max rank {node.MaxRank}.");
                    if (effect.RequiresSkill && effect.TargetSkill == null)
                        errors.Add($"Node {node.NodeId} has a skill effect without a target skill.");
                }
            }

            foreach (var node in byId.Values)
            {
                foreach (var prerequisite in node.PrerequisiteNodeIds)
                {
                    if (string.IsNullOrWhiteSpace(prerequisite))
                        errors.Add($"Node {node.NodeId} contains an empty prerequisite id.");
                    else if (!byId.ContainsKey(prerequisite))
                        errors.Add($"Node {node.NodeId} requires missing node {prerequisite}.");
                    else if (prerequisite == node.NodeId)
                        errors.Add($"Node {node.NodeId} requires itself.");
                }
            }

            var visiting = new HashSet<string>();
            var visited = new HashSet<string>();
            foreach (var nodeId in byId.Keys)
                DetectCycle(nodeId, byId, visiting, visited, errors);
            return errors;
        }

        private static void DetectCycle(string nodeId, IReadOnlyDictionary<string, SkillTreeNodeDefinition> nodesById,
            HashSet<string> visiting, HashSet<string> visited, List<string> errors)
        {
            if (visited.Contains(nodeId))
                return;
            if (!visiting.Add(nodeId))
            {
                errors.Add($"Prerequisite cycle detected at node {nodeId}.");
                return;
            }

            if (nodesById.TryGetValue(nodeId, out var node))
            {
                foreach (var prerequisite in node.PrerequisiteNodeIds)
                    if (!string.IsNullOrWhiteSpace(prerequisite) && nodesById.ContainsKey(prerequisite))
                        DetectCycle(prerequisite, nodesById, visiting, visited, errors);
            }

            visiting.Remove(nodeId);
            visited.Add(nodeId);
        }
    }
}

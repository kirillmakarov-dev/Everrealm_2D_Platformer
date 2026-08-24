using System.Collections.Generic;
using LetterHunter.SkillTree;
using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.UI.SkillTree
{
    [DisallowMultipleComponent]
    public sealed class SkillTreeGraphLayoutGroup : LayoutGroup
    {
        [SerializeField] private Vector2 cellSize = new(350f, 176f);
        [SerializeField] private Vector2 spacing = new(120f, 42f);

        public Vector2 Arrange(SkillTreeDefinition tree, IReadOnlyDictionary<string, SkillTreeNodeView> views)
        {
            if (tree == null || views == null)
                return Vector2.zero;

            var depths = new Dictionary<string, int>();
            var visiting = new HashSet<string>();
            var columns = new Dictionary<int, List<SkillTreeNodeDefinition>>();
            var maxDepth = 0;
            var maxRows = 0;

            foreach (var node in tree.Nodes)
            {
                if (node == null || !views.ContainsKey(node.NodeId))
                    continue;
                var depth = ResolveDepth(node, tree, depths, visiting);
                maxDepth = Mathf.Max(maxDepth, depth);
                if (!columns.TryGetValue(depth, out var column))
                {
                    column = new List<SkillTreeNodeDefinition>();
                    columns[depth] = column;
                }
                column.Add(node);
                maxRows = Mathf.Max(maxRows, column.Count);
            }

            var width = padding.horizontal + (maxDepth + 1) * cellSize.x + maxDepth * spacing.x;
            var height = padding.vertical + maxRows * cellSize.y + Mathf.Max(0, maxRows - 1) * spacing.y;

            foreach (var pair in columns)
            {
                var columnHeight = pair.Value.Count * cellSize.y + Mathf.Max(0, pair.Value.Count - 1) * spacing.y;
                for (var row = 0; row < pair.Value.Count; row++)
                {
                    var node = pair.Value[row];
                    if (!views.TryGetValue(node.NodeId, out var view) || view.transform is not RectTransform rect)
                        continue;

                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = cellSize;
                    rect.anchoredPosition = new Vector2(
                        -width * 0.5f + padding.left + cellSize.x * 0.5f + pair.Key * (cellSize.x + spacing.x),
                        columnHeight * 0.5f - cellSize.y * 0.5f - row * (cellSize.y + spacing.y));
                }
            }

            return new Vector2(width, height);
        }

        public override void CalculateLayoutInputHorizontal() => base.CalculateLayoutInputHorizontal();
        public override void CalculateLayoutInputVertical() { }
        public override void SetLayoutHorizontal() { }
        public override void SetLayoutVertical() { }

        private static int ResolveDepth(SkillTreeNodeDefinition node, SkillTreeDefinition tree,
            IDictionary<string, int> depths, ISet<string> visiting)
        {
            if (depths.TryGetValue(node.NodeId, out var cached))
                return cached;
            if (!visiting.Add(node.NodeId))
                return 0;

            var depth = 0;
            foreach (var prerequisiteId in node.PrerequisiteNodeIds)
                if (tree.TryGetNode(prerequisiteId, out var prerequisite))
                    depth = Mathf.Max(depth, ResolveDepth(prerequisite, tree, depths, visiting) + 1);

            visiting.Remove(node.NodeId);
            depths[node.NodeId] = depth;
            return depth;
        }
    }
}

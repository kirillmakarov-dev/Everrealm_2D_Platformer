using System.Collections.Generic;
using LetterHunter.SkillTree;
using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.UI.SkillTree
{
    [DisallowMultipleComponent]
    public sealed class SkillTreeGraphLayoutGroup : LayoutGroup
    {
        [SerializeField] private Vector2 nodeSize = new(190f, 150f);
        [SerializeField] private Vector2 contentPadding = new(110f, 90f);

        public Vector2 Arrange(ProfessionDefinitionSO profession,
            IReadOnlyDictionary<string, SkillTreeNodeView> views)
        {
            if (profession == null || views == null) return Vector2.zero;
            var maxX = 0f;
            var maxY = 0f;
            foreach (var node in profession.SkillNodes)
            {
                if (node == null || !views.TryGetValue(node.NodeId, out var view) ||
                    view.transform is not RectTransform rect) continue;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = nodeSize;
                rect.anchoredPosition = new Vector2(contentPadding.x + node.UiPosition.x,
                    -contentPadding.y - node.UiPosition.y);
                maxX = Mathf.Max(maxX, node.UiPosition.x);
                maxY = Mathf.Max(maxY, node.UiPosition.y);
            }
            return new Vector2(maxX + nodeSize.x + contentPadding.x * 2f,
                maxY + nodeSize.y + contentPadding.y * 2f);
        }

        public override void CalculateLayoutInputHorizontal() => base.CalculateLayoutInputHorizontal();
        public override void CalculateLayoutInputVertical() { }
        public override void SetLayoutHorizontal() { }
        public override void SetLayoutVertical() { }
    }
}

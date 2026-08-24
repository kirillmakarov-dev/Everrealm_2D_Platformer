using System;
using System.Collections.Generic;
using LetterHunter.Skills;
using UnityEngine;

namespace LetterHunter.SkillTree
{
    [CreateAssetMenu(menuName = "Letter Hunter/Skill Tree/Skill Node Definition")]
    public sealed class SkillNodeDefinitionSO : ScriptableObject
    {
        [SerializeField] private string nodeId;
        [SerializeField] private string displayName;
        [TextArea(3, 8), SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [Tooltip("Existing gameplay content granted after purchase.")]
        [SerializeField] private SkillDefinition abilityToGrant;
        [Min(1), SerializeField] private int requiredLevel = 1;
        [Min(0), SerializeField] private int price;
        [SerializeField] private SkillNodeDefinitionSO[] parentNodes = Array.Empty<SkillNodeDefinitionSO>();
        [SerializeField] private Vector2 uiPosition;

        public string NodeId => string.IsNullOrWhiteSpace(nodeId) ? name : nodeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description ?? string.Empty;
        public Sprite Icon => icon != null ? icon : abilityToGrant != null ? abilityToGrant.Icon : null;
        public SkillDefinition AbilityToGrant => abilityToGrant;
        public int RequiredLevel => Mathf.Max(1, requiredLevel);
        public int Price => Mathf.Max(0, price);
        public IReadOnlyList<SkillNodeDefinitionSO> ParentNodes => parentNodes;
        public Vector2 UiPosition => uiPosition;
    }
}

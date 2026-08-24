using System;
using System.Collections.Generic;
using LetterHunter.Skills;
using UnityEngine;

namespace LetterHunter.SkillTree
{
    public enum SkillTreeUnlockAction { None, UnlockSkill }
    public enum SkillTreeNodeType { Skill, Passive, Upgrade }

    public enum SkillTreeRankEffectType
    {
        AttackPower,
        Defense,
        AttackSpeed,
        SkillDamagePercent,
        SkillCooldownReductionPercent,
        SkillManaCostReductionPercent,
        MoveSpeed,
        JumpHeight
    }

    [Serializable]
    public sealed class SkillTreeRankEffectDefinition
    {
        [SerializeField] private SkillTreeRankEffectType effectType;
        [SerializeField] private SkillDefinition targetSkill;
        [SerializeField] private float amountPerRank;
        [Min(1), SerializeField] private int firstAppliedRank = 1;

        public SkillTreeRankEffectType EffectType => effectType;
        public SkillDefinition TargetSkill => targetSkill;
        public float AmountPerRank => amountPerRank;
        public int FirstAppliedRank => Mathf.Max(1, firstAppliedRank);
        public bool RequiresSkill => effectType is SkillTreeRankEffectType.SkillDamagePercent or
            SkillTreeRankEffectType.SkillCooldownReductionPercent or
            SkillTreeRankEffectType.SkillManaCostReductionPercent;
        public bool IsValid => amountPerRank != 0f && (!RequiresSkill || targetSkill != null);
    }

    [Serializable]
    public sealed class SkillTreeNodeDefinition
    {
        [SerializeField] private string nodeId;
        [SerializeField] private string displayName;
        [SerializeField] private SkillTreeNodeType nodeType;
        [SerializeField] private Sprite icon;
        [TextArea, SerializeField] private string description;
        [Min(0), SerializeField] private int goldCost;
        [SerializeField] private SkillTreeMaterialCost[] materialCosts = Array.Empty<SkillTreeMaterialCost>();
        [SerializeField] private string[] prerequisiteNodeIds = Array.Empty<string>();
        [SerializeField] private SkillTreeUnlockAction unlockAction = SkillTreeUnlockAction.UnlockSkill;
        [SerializeField] private SkillDefinition skillToUnlock;
        [SerializeField] private Vector2 uiPosition;
        [Min(1), SerializeField] private int maxRank = 1;
        [SerializeField] private SkillTreeRankEffectDefinition[] rankEffects = Array.Empty<SkillTreeRankEffectDefinition>();

        public string NodeId => string.IsNullOrWhiteSpace(nodeId) ? displayName : nodeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? NodeId : displayName;
        public SkillTreeNodeType NodeType => nodeType;
        public Sprite Icon => icon != null ? icon : skillToUnlock != null ? skillToUnlock.Icon : null;
        public string Description => description;
        public int GoldCost => Mathf.Max(0, goldCost);
        public IReadOnlyList<SkillTreeMaterialCost> MaterialCosts => materialCosts;
        public IReadOnlyList<string> PrerequisiteNodeIds => prerequisiteNodeIds;
        public SkillTreeUnlockAction UnlockAction => unlockAction;
        public SkillDefinition SkillToUnlock => skillToUnlock;
        public Vector2 UiPosition => uiPosition;
        public int MaxRank => Mathf.Max(1, maxRank);
        public IReadOnlyList<SkillTreeRankEffectDefinition> RankEffects => rankEffects;
    }
}

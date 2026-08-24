using System;
using System.Collections.Generic;
using UnityEngine;

namespace LetterHunter.SkillTree
{
    [CreateAssetMenu(menuName = "Letter Hunter/Skill Tree/Profession Definition")]
    public sealed class ProfessionDefinitionSO : ScriptableObject
    {
        [SerializeField] private string professionId;
        [SerializeField] private string displayName;
        [TextArea(2, 6), SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private SkillNodeDefinitionSO[] skillNodes = Array.Empty<SkillNodeDefinitionSO>();

        public string ProfessionId => string.IsNullOrWhiteSpace(professionId) ? name : professionId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description ?? string.Empty;
        public Sprite Icon => icon;
        public IReadOnlyList<SkillNodeDefinitionSO> SkillNodes => skillNodes;

        public bool TryGetNode(string nodeId, out SkillNodeDefinitionSO node)
        {
            node = null;
            if (string.IsNullOrWhiteSpace(nodeId)) return false;
            foreach (var candidate in skillNodes ?? Array.Empty<SkillNodeDefinitionSO>())
                if (candidate != null && candidate.NodeId == nodeId) { node = candidate; return true; }
            return false;
        }

        public bool Contains(SkillNodeDefinitionSO node)
        {
            if (node == null) return false;
            foreach (var candidate in skillNodes ?? Array.Empty<SkillNodeDefinitionSO>())
                if (candidate == node) return true;
            return false;
        }
    }
}

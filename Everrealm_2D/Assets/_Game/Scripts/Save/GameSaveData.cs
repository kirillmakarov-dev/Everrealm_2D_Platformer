using System;
using System.Collections.Generic;

namespace LetterHunter.Save
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int gold;
        public List<InventorySlotSaveData> inventorySlots = new();
        public SkillTreeSaveBlock skillTree = new();
        // Legacy fields are retained only for migration of saves created before schema v1.
        public List<string> unlockedSkillTreeNodeIds = new();
        public List<SkillTreeNodeRankSaveData> skillTreeNodeRanks = new();
        public List<SkillSlotSaveData> skillBarSlots = new();
    }

    [Serializable]
    public sealed class SkillTreeSaveBlock
    {
        public int schemaVersion;
        public string activeProfessionId;
        public List<PurchasedSkillNodeSaveData> purchasedNodes = new();
    }

    [Serializable]
    public sealed class PurchasedSkillNodeSaveData
    {
        public string professionId;
        public string nodeId;
    }

    [Serializable]
    public sealed class SkillTreeNodeRankSaveData
    {
        public string nodeId;
        public int rank;
    }

    [Serializable]
    public sealed class InventorySlotSaveData
    {
        public int slotIndex;
        public string itemId;
        public int amount;
    }

    [Serializable]
    public sealed class SkillSlotSaveData
    {
        public int slotIndex;
        public string skillId;
        public bool isExplicitEmpty;
    }
}

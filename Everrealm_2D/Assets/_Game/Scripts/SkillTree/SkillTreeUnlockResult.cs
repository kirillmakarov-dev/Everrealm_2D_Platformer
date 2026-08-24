namespace LetterHunter.SkillTree
{
    public enum SkillTreeUnlockFailure
    {
        None,
        TreeMissing,
        NodeMissing,
        AlreadyUnlocked,
        MaxRankReached,
        MissingPrerequisite,
        NotEnoughGold,
        NotEnoughMaterials
    }

    public enum SkillTreeRespecFailure
    {
        None,
        TreeMissing,
        NodeMissing,
        NotUnlocked,
        HasUnlockedDependents,
        InventoryFull
    }

    public readonly struct SkillTreeRespecResult
    {
        private SkillTreeRespecResult(bool success, SkillTreeRespecFailure failure, string nodeId, int refundedGold)
        {
            Success = success;
            Failure = failure;
            NodeId = nodeId ?? string.Empty;
            RefundedGold = refundedGold;
        }

        public bool Success { get; }
        public SkillTreeRespecFailure Failure { get; }
        public string NodeId { get; }
        public int RefundedGold { get; }

        public static SkillTreeRespecResult Succeeded(string nodeId, int refundedGold) =>
            new(true, SkillTreeRespecFailure.None, nodeId, refundedGold);

        public static SkillTreeRespecResult Failed(SkillTreeRespecFailure failure, string nodeId = "") =>
            new(false, failure, nodeId, 0);
    }

    public readonly struct SkillTreeUnlockResult
    {
        private SkillTreeUnlockResult(bool success, SkillTreeUnlockFailure failure, string nodeId)
        {
            Success = success;
            Failure = failure;
            NodeId = nodeId ?? string.Empty;
        }

        public bool Success { get; }
        public SkillTreeUnlockFailure Failure { get; }
        public string NodeId { get; }

        public static SkillTreeUnlockResult Succeeded(string nodeId) => new(true, SkillTreeUnlockFailure.None, nodeId);
        public static SkillTreeUnlockResult Failed(SkillTreeUnlockFailure failure, string nodeId = "") => new(false, failure, nodeId);
    }
}

namespace LetterHunter.SkillTree
{
    public enum SkillTreeNodeState { Locked, Available, Purchased, UnavailableByFunds }
    public enum SkillTreePurchaseFailure
    {
        None, ProfessionMissing, NodeMissing, AlreadyPurchased, RequiredLevel,
        MissingParent, NotEnoughCurrency, GrantFailed
    }

    public readonly struct SkillTreePurchaseResult
    {
        private SkillTreePurchaseResult(bool success, SkillTreePurchaseFailure failure)
        { Success = success; Failure = failure; }
        public bool Success { get; }
        public SkillTreePurchaseFailure Failure { get; }
        public static SkillTreePurchaseResult Succeeded() => new(true, SkillTreePurchaseFailure.None);
        public static SkillTreePurchaseResult Failed(SkillTreePurchaseFailure failure) => new(false, failure);
    }
}

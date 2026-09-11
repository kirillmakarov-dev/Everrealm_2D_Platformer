namespace LetterHunter.Economy
{
    public enum BuyFailureReason { None, InvalidItem, InvalidAmount, NotInCatalog, NotEnoughGold, InventoryFull }

    public readonly struct BuyResult
    {
        private BuyResult(bool success, BuyFailureReason failure, int goldSpent)
        {
            Success = success;
            Failure = failure;
            GoldSpent = goldSpent;
        }

        public bool Success { get; }
        public BuyFailureReason Failure { get; }
        public int GoldSpent { get; }

        public static BuyResult Succeeded(int goldSpent) => new(true, BuyFailureReason.None, goldSpent);
        public static BuyResult Failed(BuyFailureReason failure) => new(false, failure, 0);
    }
}

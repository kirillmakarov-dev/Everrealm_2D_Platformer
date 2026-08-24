namespace LetterHunter.Economy
{
    public enum SellFailureReason { None, InvalidItem, InvalidAmount, CannotSell, NotEnoughItems }

    public readonly struct SellResult
    {
        private SellResult(bool success, SellFailureReason failure, int goldEarned)
        {
            Success = success;
            Failure = failure;
            GoldEarned = goldEarned;
        }

        public bool Success { get; }
        public SellFailureReason Failure { get; }
        public int GoldEarned { get; }

        public static SellResult Succeeded(int goldEarned) => new(true, SellFailureReason.None, goldEarned);
        public static SellResult Failed(SellFailureReason failure) => new(false, failure, 0);
    }
}

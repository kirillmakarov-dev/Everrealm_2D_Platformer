namespace LetterHunter.Items
{
    public enum InventoryAddStatus { AddedAll, AddedPartial, NoSpace, InvalidStack }

    public readonly struct InventoryAddResult
    {
        private InventoryAddResult(InventoryAddStatus status, int requestedAmount, int addedAmount)
        {
            Status = status;
            RequestedAmount = requestedAmount;
            AddedAmount = addedAmount;
        }

        public InventoryAddStatus Status { get; }
        public int RequestedAmount { get; }
        public int AddedAmount { get; }
        public int RemainingAmount => System.Math.Max(0, RequestedAmount - AddedAmount);
        public bool AddedEverything => Status == InventoryAddStatus.AddedAll;
        public bool AddedAnything => AddedAmount > 0;

        public static InventoryAddResult Invalid(int requestedAmount) =>
            new(InventoryAddStatus.InvalidStack, requestedAmount, 0);

        public static InventoryAddResult FromAmounts(int requestedAmount, int addedAmount)
        {
            if (requestedAmount <= 0)
                return Invalid(requestedAmount);
            if (addedAmount >= requestedAmount)
                return new InventoryAddResult(InventoryAddStatus.AddedAll, requestedAmount, requestedAmount);
            if (addedAmount > 0)
                return new InventoryAddResult(InventoryAddStatus.AddedPartial, requestedAmount, addedAmount);
            return new InventoryAddResult(InventoryAddStatus.NoSpace, requestedAmount, 0);
        }
    }
}

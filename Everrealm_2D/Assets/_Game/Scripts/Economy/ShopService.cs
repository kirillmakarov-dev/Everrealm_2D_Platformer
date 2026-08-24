using LetterHunter.Items;

namespace LetterHunter.Economy
{
    public sealed class ShopService
    {
        private readonly Inventory _inventory;
        private readonly CurrencyWallet _wallet;

        public ShopService(Inventory inventory, CurrencyWallet wallet)
        {
            _inventory = inventory;
            _wallet = wallet;
        }

        public SellResult TrySell(ItemDefinition item, int amount)
        {
            if (item == null)
                return SellResult.Failed(SellFailureReason.InvalidItem);
            if (amount <= 0)
                return SellResult.Failed(SellFailureReason.InvalidAmount);
            if (!item.CanSell)
                return SellResult.Failed(SellFailureReason.CannotSell);
            if (_inventory == null || _wallet == null || _inventory.Count(item) < amount)
                return SellResult.Failed(SellFailureReason.NotEnoughItems);

            if (!_inventory.TryRemove(item, amount))
                return SellResult.Failed(SellFailureReason.NotEnoughItems);

            var earned = item.SellPrice * amount;
            _wallet.AddGold(earned);
            return SellResult.Succeeded(earned);
        }
    }
}

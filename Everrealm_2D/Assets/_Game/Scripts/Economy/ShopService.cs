using LetterHunter.Items;

namespace LetterHunter.Economy
{
    public sealed class ShopService
    {
        private readonly Inventory _inventory;
        private readonly CurrencyWallet _wallet;
        private readonly ShopCatalogDefinition _catalog;

        public ShopService(Inventory inventory, CurrencyWallet wallet, ShopCatalogDefinition catalog = null)
        {
            _inventory = inventory;
            _wallet = wallet;
            _catalog = catalog;
        }

        public BuyResult TryBuy(ItemDefinition item, int amount)
        {
            if (item == null)
                return BuyResult.Failed(BuyFailureReason.InvalidItem);
            if (amount <= 0)
                return BuyResult.Failed(BuyFailureReason.InvalidAmount);
            if (_catalog == null || !_catalog.TryGetPrice(item, out var unitPrice))
                return BuyResult.Failed(BuyFailureReason.NotInCatalog);
            if (_inventory == null || _wallet == null || !_inventory.CanAdd(new ItemStack(item, amount)))
                return BuyResult.Failed(BuyFailureReason.InventoryFull);

            var totalPrice = unitPrice * amount;
            if (!_wallet.TrySpendGold(totalPrice))
                return BuyResult.Failed(BuyFailureReason.NotEnoughGold);
            var added = _inventory.TryAdd(new ItemStack(item, amount));
            if (!added.AddedEverything)
            {
                _wallet.AddGold(totalPrice);
                return BuyResult.Failed(BuyFailureReason.InventoryFull);
            }

            return BuyResult.Succeeded(totalPrice);
        }

        public SellResult TrySell(ItemDefinition item, int amount)
        {
            if (item == null)
                return SellResult.Failed(SellFailureReason.InvalidItem);
            if (amount <= 0)
                return SellResult.Failed(SellFailureReason.InvalidAmount);
            if (_catalog != null && !_catalog.CanSell(item))
                return SellResult.Failed(SellFailureReason.NotInCatalog);
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

using LetterHunter.Economy;
using LetterHunter.Items;
using UnityEngine;

namespace LetterHunter.Loot
{
    public enum LootPickupKind { Coins, ItemStack }

    [RequireComponent(typeof(Collider2D))]
    public sealed class LootPickup2D : MonoBehaviour
    {
        [SerializeField] private LootPickupKind pickupKind;
        [Min(0), SerializeField] private int coins;
        [SerializeField] private ItemDefinition item;
        [Min(1), SerializeField] private int amount = 1;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private bool destroyAfterCollect = true;

        private void Awake()
        {
            var pickupCollider = GetComponent<Collider2D>();
            pickupCollider.isTrigger = true;
            ResolveVisual();
            RefreshItemVisual();
        }

        private void OnEnable()
        {
            ResolveVisual();
            RefreshItemVisual();
        }

        public void ConfigureCoins(int value)
        {
            pickupKind = LootPickupKind.Coins;
            coins = Mathf.Max(0, value);
            item = null;
            amount = 1;
        }

        public void ConfigureItem(ItemStack stack)
        {
            pickupKind = LootPickupKind.ItemStack;
            item = stack.Item;
            amount = Mathf.Max(1, stack.Amount);
            coins = 0;
            ResolveVisual();
            RefreshItemVisual();
        }

        private void ResolveVisual()
        {
            if (visual == null)
                visual = GetComponentInChildren<SpriteRenderer>();
        }

        private void RefreshItemVisual()
        {
            if (pickupKind != LootPickupKind.ItemStack || visual == null)
                return;
            visual.sprite = item != null ? item.Icon : null;
            visual.color = Color.white;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other);
        }

        public bool TryCollect(Component collector)
        {
            if (collector == null)
                return false;

            var collected = pickupKind == LootPickupKind.Coins
                ? TryCollectCoins(collector)
                : TryCollectItem(collector);

            if (collected && destroyAfterCollect)
                Destroy(gameObject);

            return collected;
        }

        private bool TryCollectCoins(Component collector)
        {
            if (coins <= 0)
                return false;

            var wallet = collector.GetComponentInParent<CurrencyWallet>();
            if (wallet == null)
                return false;

            wallet.AddGold(coins);
            return true;
        }

        private bool TryCollectItem(Component collector)
        {
            if (item == null || amount <= 0)
                return false;

            var inventory = collector.GetComponentInParent<PlayerInventory>();
            if (inventory == null)
                return false;

            var result = inventory.TryAdd(new ItemStack(item, amount));
            return result.AddedEverything;
        }
    }
}

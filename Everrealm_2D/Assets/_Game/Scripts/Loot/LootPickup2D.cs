using LetterHunter.Economy;
using LetterHunter.Items;
using UnityEngine;

namespace LetterHunter.Loot
{
    public enum LootPickupKind { Coins, ItemStack }

    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class LootPickup2D : MonoBehaviour
    {
        [SerializeField] private LootPickupKind pickupKind;
        [Min(0), SerializeField] private int coins;
        [SerializeField] private ItemDefinition item;
        [Min(1), SerializeField] private int amount = 1;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private bool destroyAfterCollect = true;
        [Min(0f), SerializeField] private float collectionDelayAfterDrop = 0.35f;
        private float _collectableAt;
        private Component _pendingCollector;

        private void Awake()
        {
            var body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = Mathf.Max(.01f, body.gravityScale);
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // The trigger is used for collection; additional solid colliders remain
            // solid so the reward can land and stay on a platform.
            var pickupCollider = GetComponent<Collider2D>();
            pickupCollider.isTrigger = true;
            EnsureLandingCollider();
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

        public void LaunchFromDrop(Vector2 velocity, float angularVelocity)
        {
            var body = GetComponent<Rigidbody2D>();
            if (body == null) return;

            body.bodyType = RigidbodyType2D.Dynamic;
            body.linearVelocity = velocity;
            body.angularVelocity = angularVelocity;
            _collectableAt = Time.time + Mathf.Max(0f, collectionDelayAfterDrop);
        }

        private void EnsureLandingCollider()
        {
            foreach (var collider in GetComponents<Collider2D>())
                if (!collider.isTrigger)
                    return;

            var landingCollider = gameObject.AddComponent<BoxCollider2D>();
            landingCollider.size = new Vector2(.55f, .55f);
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

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_pendingCollector == other)
                _pendingCollector = null;
        }

        private void Update()
        {
            if (_pendingCollector != null && Time.time >= _collectableAt)
                TryCollect(_pendingCollector);
        }

        public bool TryCollect(Component collector)
        {
            if (collector == null)
                return false;
            if (Time.time < _collectableAt)
            {
                _pendingCollector = collector;
                return false;
            }

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

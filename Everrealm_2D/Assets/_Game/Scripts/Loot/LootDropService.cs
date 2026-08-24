using LetterHunter.Items;
using UnityEngine;

namespace LetterHunter.Loot
{
    [DisallowMultipleComponent]
    public sealed class LootDropService : MonoBehaviour
    {
        [SerializeField] private LootPickup2D coinPickupPrefab;
        [SerializeField] private LootPickup2D itemPickupPrefab;
        [Min(0f), SerializeField] private float scatterRadius = 0.6f;

        public void Drop(LootRoll roll, Vector3 origin)
        {
            if (!roll.HasAnyRewards)
                return;

            if (roll.Coins > 0)
                SpawnCoinPickup(roll.Coins, origin);

            foreach (var item in roll.Items)
                if (item.IsValid)
                    SpawnItemPickup(item, origin);
        }

        private void SpawnCoinPickup(int coins, Vector3 origin)
        {
            var pickup = CreatePickup(coinPickupPrefab, "Coin Pickup", origin);
            pickup.ConfigureCoins(coins);
        }

        private void SpawnItemPickup(ItemStack stack, Vector3 origin)
        {
            var pickup = CreatePickup(itemPickupPrefab, stack.Item.DisplayName, origin);
            pickup.ConfigureItem(stack);
        }

        private LootPickup2D CreatePickup(LootPickup2D prefab, string fallbackName, Vector3 origin)
        {
            var position = origin + (Vector3)(Random.insideUnitCircle * scatterRadius);
            if (prefab != null)
                return Instantiate(prefab, position, Quaternion.identity);

            var pickupObject = new GameObject(fallbackName);
            pickupObject.transform.position = position;
            var collider = pickupObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.35f;
            return pickupObject.AddComponent<LootPickup2D>();
        }
    }
}

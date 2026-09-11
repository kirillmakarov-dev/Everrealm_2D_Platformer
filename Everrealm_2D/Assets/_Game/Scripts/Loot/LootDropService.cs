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
        [Min(0f), SerializeField] private float dropHeight = 0.8f;
        [Min(0f), SerializeField] private float upwardLaunchSpeed = 3.8f;

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
            // Give every reward its own start point and launch impulse. The loot table
            // decides what drops; this only controls the physical presentation.
            var offset = Random.insideUnitCircle * scatterRadius;
            var position = origin + new Vector3(offset.x, dropHeight + Mathf.Max(0f, offset.y * .25f), 0f);
            // Vary the upward hop while keeping horizontal velocity at zero. This
            // makes rewards look individually spawned without letting them slide.
            var launchSpeed = Random.Range(upwardLaunchSpeed * .85f, upwardLaunchSpeed);
            if (prefab != null)
            {
                var pickup = Instantiate(prefab, position, Quaternion.identity);
                pickup.LaunchFromDrop(Vector2.up * launchSpeed, 0f);
                return pickup;
            }

            var pickupObject = new GameObject(fallbackName);
            pickupObject.transform.position = position;
            var collider = pickupObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.35f;
            var landingCollider = pickupObject.AddComponent<BoxCollider2D>();
            landingCollider.size = new Vector2(.55f, .55f);
            var fallbackPickup = pickupObject.AddComponent<LootPickup2D>();
            fallbackPickup.LaunchFromDrop(Vector2.up * launchSpeed, 0f);
            return fallbackPickup;
        }
    }
}

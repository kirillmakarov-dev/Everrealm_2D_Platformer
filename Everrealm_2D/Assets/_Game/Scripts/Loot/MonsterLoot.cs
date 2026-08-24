using LetterHunter.Effects;
using UnityEngine;

namespace LetterHunter.Loot
{
    [DisallowMultipleComponent]
    public sealed class MonsterLoot : MonoBehaviour
    {
        [SerializeField] private LootTable lootTable;
        [SerializeField] private LootDropService dropService;
        [SerializeField] private Transform dropPoint;
        [SerializeField] private bool logDrops;

        private bool _dropped;
        private IRandomSource _random;

        private void Awake()
        {
            _random = new SystemRandomSource();
            if (dropPoint == null)
                dropPoint = transform;
            if (dropService == null)
                dropService = FindAnyObjectByType<LootDropService>();
        }

        public void DropLoot()
        {
            if (_dropped || lootTable == null)
                return;

            _dropped = true;
            var roll = lootTable.Roll(_random);
            if (logDrops)
                Debug.Log($"[Loot] {name} rolled {roll.Coins} coins and {roll.Items.Count} item stack(s).", this);

            dropService?.Drop(roll, dropPoint != null ? dropPoint.position : transform.position);
        }
    }
}

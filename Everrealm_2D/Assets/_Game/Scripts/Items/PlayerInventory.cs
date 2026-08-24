using System;
using UnityEngine;

namespace LetterHunter.Items
{
    [DisallowMultipleComponent]
    public sealed class PlayerInventory : MonoBehaviour
    {
        [Header("Runtime")]
        [Min(1), SerializeField] private int capacity = 24;
        [Header("Playtest Start Items")]
        [SerializeField] private StartingInventoryItem[] startingItems = Array.Empty<StartingInventoryItem>();

        public event Action InventoryChanged;
        public Inventory Inventory { get; private set; }
        public Inventory RuntimeInventory
        {
            get
            {
                EnsureInventory();
                return Inventory;
            }
        }

        private void Awake()
        {
            EnsureInventory();
        }

        public InventoryAddResult TryAdd(ItemStack stack)
        {
            EnsureInventory();
            return Inventory.TryAdd(stack);
        }

        public bool TryRemove(ItemDefinition item, int amount)
        {
            EnsureInventory();
            return Inventory.TryRemove(item, amount);
        }

        public bool TryMoveSlot(int fromIndex, int toIndex)
        {
            EnsureInventory();
            return Inventory.TryMoveSlot(fromIndex, toIndex);
        }

        public bool TrySwapSlots(int firstIndex, int secondIndex)
        {
            EnsureInventory();
            return Inventory.TrySwapSlots(firstIndex, secondIndex);
        }

        public bool TryMergeSlots(int fromIndex, int toIndex)
        {
            EnsureInventory();
            return Inventory.TryMergeSlots(fromIndex, toIndex);
        }

        public bool TrySplitStack(int fromIndex, int toIndex, int amount)
        {
            EnsureInventory();
            return Inventory.TrySplitStack(fromIndex, toIndex, amount);
        }

        private void EnsureInventory()
        {
            if (Inventory != null)
                return;

            Inventory = new Inventory(capacity);
            Inventory.InventoryChanged += OnInventoryChanged;
            AddStartingItems();
        }

        private void AddStartingItems()
        {
            if (startingItems == null)
                return;

            foreach (var startingItem in startingItems)
            {
                if (startingItem == null || !startingItem.IsValid)
                    continue;

                Inventory.TryAdd(startingItem.ToStack());
            }
        }

        private void OnInventoryChanged()
        {
            InventoryChanged?.Invoke();
        }
    }
}

using System;
using System.Collections.Generic;

namespace LetterHunter.Items
{
    public sealed class Inventory
    {
        private readonly List<InventorySlot> _slots;

        public Inventory(int capacity)
        {
            capacity = Math.Max(1, capacity);
            _slots = new List<InventorySlot>(capacity);
            for (var i = 0; i < capacity; i++)
                _slots.Add(new InventorySlot());
        }

        public IReadOnlyList<InventorySlot> Slots => _slots;
        public int Capacity => _slots.Count;
        public event Action InventoryChanged;

        public InventoryAddResult TryAdd(ItemStack stack)
        {
            if (!stack.IsValid)
                return InventoryAddResult.Invalid(stack.Amount);

            var remaining = stack.Amount;
            remaining = MergeIntoExistingStacks(stack.Item, remaining);
            remaining = PlaceIntoEmptySlots(stack.Item, remaining);
            var result = InventoryAddResult.FromAmounts(stack.Amount, stack.Amount - remaining);
            if (result.AddedAnything)
                InventoryChanged?.Invoke();
            return result;
        }

        public bool CanAdd(ItemStack stack)
        {
            if (!stack.IsValid)
                return false;

            var remaining = stack.Amount;
            foreach (var slot in _slots)
            {
                if (!slot.IsEmpty && slot.Item == stack.Item)
                    remaining -= slot.RemainingSpace;
                else if (slot.IsEmpty)
                    remaining -= stack.Item.MaxStack;

                if (remaining <= 0)
                    return true;
            }

            return false;
        }

        public int Count(ItemDefinition item)
        {
            if (item == null)
                return 0;

            var total = 0;
            foreach (var slot in _slots)
                if (!slot.IsEmpty && slot.Item == item)
                    total += slot.Amount;

            return total;
        }

        public bool TryRemove(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0 || Count(item) < amount)
                return false;

            var remaining = amount;
            foreach (var slot in _slots)
            {
                if (slot.IsEmpty || slot.Item != item)
                    continue;

                remaining -= slot.Remove(remaining);
                if (remaining <= 0)
                {
                    InventoryChanged?.Invoke();
                    return true;
                }
            }

            InventoryChanged?.Invoke();
            return true;
        }

        public bool TryMoveSlot(int fromIndex, int toIndex)
        {
            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex)
                return false;

            var from = _slots[fromIndex];
            var to = _slots[toIndex];
            if (from.IsEmpty)
                return false;

            if (to.IsEmpty)
            {
                to.Set(from.Item, from.Amount);
                from.Clear();
                InventoryChanged?.Invoke();
                return true;
            }

            if (from.Item == to.Item)
            {
                var moved = MoveAmount(from, to, from.Amount);
                if (moved <= 0)
                    return false;

                InventoryChanged?.Invoke();
                return true;
            }

            SwapSlots(fromIndex, toIndex);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool TrySwapSlots(int firstIndex, int secondIndex)
        {
            if (!IsValidIndex(firstIndex) || !IsValidIndex(secondIndex) || firstIndex == secondIndex)
                return false;

            SwapSlots(firstIndex, secondIndex);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool TryMergeSlots(int fromIndex, int toIndex)
        {
            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex)
                return false;

            var from = _slots[fromIndex];
            var to = _slots[toIndex];
            if (from.IsEmpty || to.IsEmpty || from.Item != to.Item)
                return false;

            var moved = MoveAmount(from, to, from.Amount);
            if (moved <= 0)
                return false;

            InventoryChanged?.Invoke();
            return true;
        }

        public bool TrySplitStack(int fromIndex, int toIndex, int amount)
        {
            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex || amount <= 0)
                return false;

            var from = _slots[fromIndex];
            var to = _slots[toIndex];
            if (from.IsEmpty || from.Amount <= amount)
                return false;

            if (to.IsEmpty)
            {
                var moved = from.Remove(amount);
                to.Set(from.Item, moved);
                InventoryChanged?.Invoke();
                return true;
            }

            if (to.Item != from.Item || to.RemainingSpace <= 0)
                return false;

            var merged = MoveAmount(from, to, amount);
            if (merged <= 0)
                return false;

            InventoryChanged?.Invoke();
            return true;
        }

        public bool IsValidIndex(int index) => index >= 0 && index < _slots.Count;

        public void Clear()
        {
            foreach (var slot in _slots)
                slot.Clear();

            InventoryChanged?.Invoke();
        }

        public bool TrySetSlot(int index, ItemDefinition item, int amount)
        {
            if (!IsValidIndex(index))
                return false;

            _slots[index].Set(item, amount);
            InventoryChanged?.Invoke();
            return true;
        }

        private int MergeIntoExistingStacks(ItemDefinition item, int amount)
        {
            foreach (var slot in _slots)
            {
                if (amount <= 0)
                    break;
                if (slot.IsEmpty || slot.Item != item)
                    continue;

                amount -= slot.Add(amount);
            }

            return amount;
        }

        private int PlaceIntoEmptySlots(ItemDefinition item, int amount)
        {
            foreach (var slot in _slots)
            {
                if (amount <= 0)
                    break;
                if (!slot.IsEmpty)
                    continue;

                var added = Math.Min(amount, item.MaxStack);
                slot.Set(item, added);
                amount -= added;
            }

            return amount;
        }

        private void SwapSlots(int firstIndex, int secondIndex)
        {
            var first = _slots[firstIndex];
            var second = _slots[secondIndex];
            var firstStack = first.Stack;
            var secondStack = second.Stack;

            first.Set(secondStack.Item, secondStack.Amount);
            second.Set(firstStack.Item, firstStack.Amount);
        }

        private static int MoveAmount(InventorySlot from, InventorySlot to, int requestedAmount)
        {
            if (from.IsEmpty || to.IsEmpty || from.Item != to.Item || requestedAmount <= 0)
                return 0;

            var moved = Math.Min(requestedAmount, to.RemainingSpace);
            moved = Math.Min(moved, from.Amount);
            if (moved <= 0)
                return 0;

            from.Remove(moved);
            to.Add(moved);
            return moved;
        }
    }
}

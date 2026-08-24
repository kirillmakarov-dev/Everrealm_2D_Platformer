using System;
using System.Collections.Generic;
using LetterHunter.Economy;
using LetterHunter.Items;
using LetterHunter.Skills;

namespace LetterHunter.SkillTree
{
    public sealed class SkillTreeService
    {
        private readonly SkillTreeDefinition _definition;
        private readonly SkillTreeProgress _progress;
        private readonly CurrencyWallet _wallet;
        private readonly Inventory _inventory;
        private readonly Action<SkillDefinition> _skillUnlocked;
        private readonly Action<SkillTreeNodeDefinition, int> _rankChanged;

        public SkillTreeService(SkillTreeDefinition definition, SkillTreeProgress progress,
            CurrencyWallet wallet, Inventory inventory, Action<SkillDefinition> skillUnlocked,
            Action<SkillTreeNodeDefinition, int> rankChanged = null)
        {
            _definition = definition;
            _progress = progress ?? new SkillTreeProgress();
            _wallet = wallet;
            _inventory = inventory;
            _skillUnlocked = skillUnlocked;
            _rankChanged = rankChanged;
        }

        public SkillTreeProgress Progress => _progress;

        public bool CanUnlock(string nodeId, out SkillTreeUnlockFailure failure)
        {
            failure = Validate(nodeId, out _);
            return failure == SkillTreeUnlockFailure.None;
        }

        public SkillTreeUnlockResult TryUnlock(string nodeId)
        {
            var failure = Validate(nodeId, out var node);
            if (failure != SkillTreeUnlockFailure.None)
                return SkillTreeUnlockResult.Failed(failure, nodeId);

            if (node.GoldCost > 0 && !_wallet.TrySpendGold(node.GoldCost))
                return SkillTreeUnlockResult.Failed(SkillTreeUnlockFailure.NotEnoughGold, nodeId);

            foreach (var cost in node.MaterialCosts)
            {
                if (cost.IsValid)
                    _inventory.TryRemove(cost.Item, cost.Amount);
            }

            var previousRank = _progress.GetRank(node.NodeId);
            _progress.SetRank(node.NodeId, previousRank + 1);
            if (previousRank == 0 && node.UnlockAction == SkillTreeUnlockAction.UnlockSkill && node.SkillToUnlock != null)
                _skillUnlocked?.Invoke(node.SkillToUnlock);
            _rankChanged?.Invoke(node, previousRank + 1);

            return SkillTreeUnlockResult.Succeeded(node.NodeId);
        }

        public bool CanRefundRank(string nodeId, out SkillTreeRespecFailure failure)
        {
            failure = ValidateRefund(nodeId, out _);
            return failure == SkillTreeRespecFailure.None;
        }

        public SkillTreeRespecResult TryRefundRank(string nodeId)
        {
            var failure = ValidateRefund(nodeId, out var node);
            if (failure != SkillTreeRespecFailure.None)
                return SkillTreeRespecResult.Failed(failure, nodeId);

            var currentRank = _progress.GetRank(node.NodeId);
            var refundedGold = (int)Math.Floor(node.GoldCost * _definition.GoldRefundRate);
            _progress.SetRank(node.NodeId, currentRank - 1);
            if (refundedGold > 0)
                _wallet?.AddGold(refundedGold);

            if (_definition.RefundMaterials)
            {
                foreach (var cost in node.MaterialCosts)
                    if (cost.IsValid)
                        _inventory.TryAdd(new ItemStack(cost.Item, cost.Amount));
            }

            _rankChanged?.Invoke(node, currentRank - 1);
            return SkillTreeRespecResult.Succeeded(node.NodeId, refundedGold);
        }

        private SkillTreeUnlockFailure Validate(string nodeId, out SkillTreeNodeDefinition node)
        {
            node = null;
            if (_definition == null)
                return SkillTreeUnlockFailure.TreeMissing;
            if (!_definition.TryGetNode(nodeId, out node))
                return SkillTreeUnlockFailure.NodeMissing;
            if (_progress.GetRank(node.NodeId) >= node.MaxRank)
                return node.MaxRank == 1
                    ? SkillTreeUnlockFailure.AlreadyUnlocked
                    : SkillTreeUnlockFailure.MaxRankReached;

            foreach (var prerequisite in node.PrerequisiteNodeIds)
                if (!string.IsNullOrWhiteSpace(prerequisite) && !_progress.IsUnlocked(prerequisite))
                    return SkillTreeUnlockFailure.MissingPrerequisite;

            if (_wallet == null || !_wallet.CanSpendGold(node.GoldCost))
                return SkillTreeUnlockFailure.NotEnoughGold;

            foreach (var cost in node.MaterialCosts)
            {
                if (!cost.IsValid)
                    continue;
                if (_inventory == null || _inventory.Count(cost.Item) < cost.Amount)
                    return SkillTreeUnlockFailure.NotEnoughMaterials;
            }

            return SkillTreeUnlockFailure.None;
        }

        private SkillTreeRespecFailure ValidateRefund(string nodeId, out SkillTreeNodeDefinition node)
        {
            node = null;
            if (_definition == null)
                return SkillTreeRespecFailure.TreeMissing;
            if (!_definition.TryGetNode(nodeId, out node))
                return SkillTreeRespecFailure.NodeMissing;

            var currentRank = _progress.GetRank(node.NodeId);
            if (currentRank <= 0)
                return SkillTreeRespecFailure.NotUnlocked;
            if (currentRank == 1 && HasUnlockedDependent(node.NodeId))
                return SkillTreeRespecFailure.HasUnlockedDependents;
            if (_definition.RefundMaterials && !CanRefundMaterials(node))
                return SkillTreeRespecFailure.InventoryFull;
            return SkillTreeRespecFailure.None;
        }

        private bool HasUnlockedDependent(string nodeId)
        {
            foreach (var candidate in _definition.Nodes)
            {
                if (candidate == null || !_progress.IsUnlocked(candidate.NodeId))
                    continue;
                foreach (var prerequisite in candidate.PrerequisiteNodeIds)
                    if (prerequisite == nodeId)
                        return true;
            }

            return false;
        }

        private bool CanRefundMaterials(SkillTreeNodeDefinition node)
        {
            var required = new Dictionary<ItemDefinition, int>();
            foreach (var cost in node.MaterialCosts)
            {
                if (!cost.IsValid)
                    continue;
                required.TryGetValue(cost.Item, out var amount);
                required[cost.Item] = amount + cost.Amount;
            }

            if (required.Count == 0)
                return true;
            if (_inventory == null)
                return false;

            var emptySlots = 0;
            foreach (var slot in _inventory.Slots)
                if (slot.IsEmpty)
                    emptySlots++;

            var neededSlots = 0;
            foreach (var pair in required)
            {
                var remaining = pair.Value;
                foreach (var slot in _inventory.Slots)
                    if (!slot.IsEmpty && slot.Item == pair.Key)
                        remaining -= slot.RemainingSpace;
                if (remaining > 0)
                    neededSlots += (int)Math.Ceiling(remaining / (double)pair.Key.MaxStack);
            }

            return neededSlots <= emptySlots;
        }
    }
}

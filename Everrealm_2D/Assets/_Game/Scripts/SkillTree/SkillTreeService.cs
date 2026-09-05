using System;
using LetterHunter.Economy;
using LetterHunter.Skills;

namespace LetterHunter.SkillTree
{
    public sealed class SkillTreeService
    {
        private readonly SkillTreeProgress _progress;
        private readonly CurrencyWallet _wallet;
        private readonly Func<int> _currentLevel;
        private readonly Func<SkillDefinition, bool> _alreadyOwnsAbility;
        private readonly Action<SkillDefinition> _grantAbility;

        public SkillTreeService(SkillTreeProgress progress, CurrencyWallet wallet, Func<int> currentLevel,
            Func<SkillDefinition, bool> alreadyOwnsAbility, Action<SkillDefinition> grantAbility)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _wallet = wallet;
            _currentLevel = currentLevel ?? (() => 1);
            _alreadyOwnsAbility = alreadyOwnsAbility ?? (_ => false);
            _grantAbility = grantAbility;
        }

        public ProfessionDefinitionSO ActiveProfession { get; private set; }
        public SkillTreeProgress Progress => _progress;
        public int CurrentLevel => Math.Max(1, _currentLevel());
        public int CurrentCoins => _wallet != null ? _wallet.Gold : 0;
        public event Action Changed;
        public event Action<ProfessionDefinitionSO, SkillNodeDefinitionSO> NodePurchased;
        public bool IsPurchasing { get; private set; }

        public void SetActiveProfession(ProfessionDefinitionSO profession)
        {
            ActiveProfession = profession;
            _progress.SetActiveProfession(profession != null ? profession.ProfessionId : string.Empty, false);
            Changed?.Invoke();
        }

        public SkillTreeNodeState GetState(SkillNodeDefinitionSO node)
        {
            if (ActiveProfession == null || node == null || !ActiveProfession.Contains(node))
                return SkillTreeNodeState.Locked;
            if (_progress.IsPurchased(ActiveProfession.ProfessionId, node.NodeId))
                return SkillTreeNodeState.Purchased;
            if (CurrentLevel < node.RequiredLevel)
                return SkillTreeNodeState.Locked;
            foreach (var parent in node.ParentNodes)
                if (parent != null && !_progress.IsPurchased(ActiveProfession.ProfessionId, parent.NodeId))
                    return SkillTreeNodeState.Locked;
            return _wallet != null && _wallet.CanSpendGold(node.Price)
                ? SkillTreeNodeState.Available
                : SkillTreeNodeState.UnavailableByFunds;
        }

        public SkillTreePurchaseResult TryPurchase(SkillNodeDefinitionSO node)
        {
            if (IsPurchasing) return SkillTreePurchaseResult.Failed(SkillTreePurchaseFailure.GrantFailed);
            SkillTreePurchaseResult result;
            IsPurchasing = true;
            try { result = PurchaseCore(node); }
            finally { IsPurchasing = false; }
            if (result.Success)
            {
                NodePurchased?.Invoke(ActiveProfession, node);
                Changed?.Invoke();
            }
            return result;
        }

        private SkillTreePurchaseResult PurchaseCore(SkillNodeDefinitionSO node)
        {
            if (ActiveProfession == null) return SkillTreePurchaseResult.Failed(SkillTreePurchaseFailure.ProfessionMissing);
            if (node == null || !ActiveProfession.Contains(node)) return SkillTreePurchaseResult.Failed(SkillTreePurchaseFailure.NodeMissing);
            if (_progress.IsPurchased(ActiveProfession.ProfessionId, node.NodeId)) return SkillTreePurchaseResult.Failed(SkillTreePurchaseFailure.AlreadyPurchased);
            if (CurrentLevel < node.RequiredLevel) return SkillTreePurchaseResult.Failed(SkillTreePurchaseFailure.RequiredLevel);
            foreach (var parent in node.ParentNodes)
                if (parent != null && !_progress.IsPurchased(ActiveProfession.ProfessionId, parent.NodeId))
                    return SkillTreePurchaseResult.Failed(SkillTreePurchaseFailure.MissingParent);
            if (_wallet == null || !_wallet.TrySpendGold(node.Price))
                return SkillTreePurchaseResult.Failed(SkillTreePurchaseFailure.NotEnoughCurrency);

            var professionId = ActiveProfession.ProfessionId;
            try
            {
                if (!_progress.MarkPurchased(professionId, node.NodeId, false))
                {
                    _wallet.AddGold(node.Price);
                    return SkillTreePurchaseResult.Failed(SkillTreePurchaseFailure.AlreadyPurchased);
                }
                if (node.AbilityToGrant != null && !_alreadyOwnsAbility(node.AbilityToGrant))
                    _grantAbility?.Invoke(node.AbilityToGrant);
            }
            catch (Exception)
            {
                _progress.RemovePurchased(professionId, node.NodeId, false);
                _wallet.AddGold(node.Price);
                return SkillTreePurchaseResult.Failed(SkillTreePurchaseFailure.GrantFailed);
            }

            return SkillTreePurchaseResult.Succeeded();
        }

        public void NotifyExternalStateChanged() => Changed?.Invoke();
    }
}

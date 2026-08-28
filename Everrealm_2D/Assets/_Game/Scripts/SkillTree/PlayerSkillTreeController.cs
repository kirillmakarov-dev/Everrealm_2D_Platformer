using System;
using System.Collections.Generic;
using LetterHunter.Characters;
using LetterHunter.Core;
using LetterHunter.Economy;
using LetterHunter.Skills;
using LetterHunter.UI.Skills;
using UnityEngine;
using UnityEngine.Serialization;

namespace LetterHunter.SkillTree
{
    [DisallowMultipleComponent]
    public sealed class PlayerSkillTreeController : MonoBehaviour
    {
        [Header("Profession domain")]
        [SerializeField] private ProfessionDefinitionSO startingProfession;
        [SerializeField] private ProfessionDefinitionSO[] availableProfessions = Array.Empty<ProfessionDefinitionSO>();
        [FormerlySerializedAs("skillTree"), SerializeField, HideInInspector]
        private SkillTreeDefinition legacySkillTree;

        [Header("Existing gameplay adapters")]
        [SerializeField] private PlayerClassController player;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private SkillBarPresenter skillBar;

        private readonly SkillTreeProgress _progress = new();
        private SkillTreeService _service;

        public event Action ProgressionCommitted;
        public event Action StateChanged;
        public ProfessionDefinitionSO ActiveProfession => Service.ActiveProfession;
        public IReadOnlyList<ProfessionDefinitionSO> AvailableProfessions => availableProfessions;
        public SkillTreeProgress Progress => _progress;
        public CurrencyWallet Wallet => wallet;
        public PlayerClassController Player => player;
        public int CurrentLevel => Service.CurrentLevel;
        public int CurrentCoins => Service.CurrentCoins;
        public SkillTreeDefinition SkillTree => legacySkillTree;

        public SkillTreeService Service
        {
            get { EnsureService(); return _service; }
        }

        private void Awake()
        {
            if (player == null) player = GetComponent<PlayerClassController>();
            if (player == null) player = FindFirstObjectByType<PlayerClassController>();
            if (wallet == null) wallet = GetComponent<CurrencyWallet>();
            if (skillBar == null) skillBar = FindFirstObjectByType<SkillBarPresenter>();
            EnsureService();
        }

        private void OnEnable()
        {
            if (wallet != null) wallet.GoldChanged += OnGoldChanged;
        }

        private void OnDisable()
        {
            if (wallet != null) wallet.GoldChanged -= OnGoldChanged;
        }

        public bool SetActiveProfession(string professionId)
        {
            if (!TryGetProfession(professionId, out var profession)) return false;
            Service.SetActiveProfession(profession);
            StateChanged?.Invoke();
            return true;
        }

        public SkillTreePurchaseResult TryPurchase(SkillNodeDefinitionSO node) => Service.TryPurchase(node);

        public void RestoreProgress(string activeProfessionId, IEnumerable<PurchasedSkillNode> purchased)
        {
            var entries = new List<PurchasedSkillNode>();
            if (purchased != null)
                foreach (var entry in purchased)
                    if (TryGetProfession(entry.ProfessionId, out var profession) && profession.TryGetNode(entry.NodeId, out _))
                        entries.Add(entry);

            var professionId = activeProfessionId;
            if (!TryGetProfession(professionId, out var active))
                active = ResolveStartingProfession();
            professionId = active != null ? active.ProfessionId : string.Empty;
            _progress.Replace(professionId, entries);
            Service.SetActiveProfession(active);
            RegrantPurchasedAbilities();
            StateChanged?.Invoke();
        }

        public void RestoreLegacyNodes(IEnumerable<string> legacyNodeIds)
        {
            var profession = ResolveStartingProfession();
            var entries = new List<PurchasedSkillNode>();
            if (profession != null && legacyNodeIds != null)
                foreach (var nodeId in legacyNodeIds)
                    if (profession.TryGetNode(nodeId, out _))
                        entries.Add(new PurchasedSkillNode(profession.ProfessionId, nodeId));
            RestoreProgress(profession != null ? profession.ProfessionId : string.Empty, entries);
        }

        public void ResetProgress()
        {
            var profession = ResolveStartingProfession();
            _progress.Replace(profession != null ? profession.ProfessionId : string.Empty,
                Array.Empty<PurchasedSkillNode>());
            Service.SetActiveProfession(profession);
            player?.ResetLearnedSkills();
            skillBar?.Rebuild();
            StateChanged?.Invoke();
            ProgressionCommitted?.Invoke();
        }

        public bool TryGetProfession(string professionId, out ProfessionDefinitionSO profession)
        {
            profession = null;
            if (string.IsNullOrWhiteSpace(professionId)) return false;
            if (startingProfession != null && startingProfession.ProfessionId == professionId)
            { profession = startingProfession; return true; }
            foreach (var candidate in availableProfessions ?? Array.Empty<ProfessionDefinitionSO>())
                if (candidate != null && candidate.ProfessionId == professionId)
                { profession = candidate; return true; }
            return false;
        }

        private void EnsureService()
        {
            if (_service != null) return;
            _service = new SkillTreeService(_progress, wallet, ResolveLevel, OwnsAbility, GrantAbility);
            _service.Changed += OnServiceChanged;
            _service.NodePurchased += OnNodePurchased;
            var profession = ResolveStartingProfession();
            if (!string.IsNullOrWhiteSpace(_progress.ActiveProfessionId))
            {
                if (TryGetProfession(_progress.ActiveProfessionId, out var restoredProfession))
                    profession = restoredProfession;
            }
            _service.SetActiveProfession(profession);
        }

        private ProfessionDefinitionSO ResolveStartingProfession()
        {
            if (startingProfession != null) return startingProfession;
            if (player != null)
            {
                var classId = player.ClassType.ToString();
                foreach (var profession in availableProfessions ?? Array.Empty<ProfessionDefinitionSO>())
                    if (profession != null && (profession.ProfessionId == classId.ToLowerInvariant() ||
                        string.Equals(profession.DisplayName, classId, StringComparison.OrdinalIgnoreCase)))
                        return profession;
            }
            foreach (var profession in availableProfessions ?? Array.Empty<ProfessionDefinitionSO>())
                if (profession != null) return profession;
            return null;
        }

        private int ResolveLevel() => player != null && player.Stats != null ? player.Stats.Level : 1;

        private bool OwnsAbility(SkillDefinition ability)
        {
            if (ability == null || player == null || player.SkillService == null) return false;
            foreach (var known in player.SkillService.Definitions)
                if (known != null && known.SkillId == ability.SkillId) return true;
            return false;
        }

        private void GrantAbility(SkillDefinition ability)
        {
            player?.LearnSkill(ability);
            if (ability != null && ability.SkillType is not SkillType.Passive and not SkillType.AutoAttackUpgrade)
                skillBar?.TryAutoAssignSkill(ability);
            skillBar?.Rebuild();
        }

        private void RegrantPurchasedAbilities()
        {
            foreach (var entry in _progress.EnumeratePurchased())
            {
                if (!TryGetProfession(entry.ProfessionId, out var profession) ||
                    !profession.TryGetNode(entry.NodeId, out var node) || node.AbilityToGrant == null) continue;
                if (!OwnsAbility(node.AbilityToGrant)) GrantAbility(node.AbilityToGrant);
            }
            skillBar?.Rebuild();
        }

        private void OnServiceChanged() => StateChanged?.Invoke();
        private void OnNodePurchased(ProfessionDefinitionSO profession, SkillNodeDefinitionSO node)
        {
            ProgressionCommitted?.Invoke();
        }
        private void OnGoldChanged(int _) => Service.NotifyExternalStateChanged();
    }
}

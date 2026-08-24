using LetterHunter.Characters;
using LetterHunter.Economy;
using LetterHunter.Items;
using LetterHunter.Skills;
using LetterHunter.UI.Skills;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LetterHunter.SkillTree
{
    [DisallowMultipleComponent]
    public sealed class PlayerSkillTreeController : MonoBehaviour
    {
        [SerializeField] private SkillTreeDefinition skillTree;
        [SerializeField] private PlayerClassController player;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private SkillBarPresenter skillBar;
        [SerializeField] private PlayerSkillLoadout skillLoadout;

        private SkillTreeService _service;
        private readonly SkillTreeProgress _progress = new();

        public event Action ProgressionCommitted;

        public SkillTreeDefinition SkillTree => skillTree;
        public SkillTreeProgress Progress => _progress;
        public PlayerInventory Inventory => inventory;
        public CurrencyWallet Wallet => wallet;
        public SkillTreeService Service
        {
            get
            {
                EnsureService();
                return _service;
            }
        }

        private void Awake()
        {
            if (player == null)
                player = GetComponent<PlayerClassController>();
            if (inventory == null)
                inventory = GetComponent<PlayerInventory>();
            if (wallet == null)
                wallet = GetComponent<CurrencyWallet>();
            if (skillBar == null)
                skillBar = FindFirstObjectByType<SkillBarPresenter>();
            if (skillLoadout == null)
                skillLoadout = GetComponent<PlayerSkillLoadout>();

            EnsureService();
        }

        public void SetSkillTree(SkillTreeDefinition definition)
        {
            skillTree = definition;
            _service = null;
            EnsureService();
        }

        private void EnsureService()
        {
            if (_service != null)
                return;

            _service = new SkillTreeService(skillTree, _progress, wallet,
                inventory != null ? inventory.RuntimeInventory : null, OnSkillUnlocked, OnNodeRankChanged);
        }

        public void ApplyUnlockedNodes(System.Collections.Generic.IEnumerable<string> nodeIds)
        {
            _progress.ReplaceUnlocked(nodeIds);
            RebuildLearnedSkills();
        }

        public void ApplyNodeRanks(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, int>> nodeRanks)
        {
            var validRanks = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>();
            if (nodeRanks != null && skillTree != null)
            {
                foreach (var pair in nodeRanks)
                {
                    if (!skillTree.TryGetNode(pair.Key, out var node) || pair.Value <= 0)
                        continue;
                    validRanks.Add(new System.Collections.Generic.KeyValuePair<string, int>(
                        pair.Key, Mathf.Min(pair.Value, node.MaxRank)));
                }
            }

            _progress.ReplaceRanks(validRanks);
            RebuildLearnedSkills();
        }

        private void RebuildLearnedSkills()
        {
            player?.ResetLearnedSkills();

            if (skillTree == null || player == null)
                return;

            foreach (var nodeId in _progress.UnlockedNodeIds)
            {
                if (!skillTree.TryGetNode(nodeId, out var node))
                    continue;
                if (node.UnlockAction == SkillTreeUnlockAction.UnlockSkill && node.SkillToUnlock != null)
                    player.LearnSkill(node.SkillToUnlock);
            }

            ApplyRankEffects();
            skillBar?.Rebuild();
        }

        public void ResetProgress()
        {
            ApplyUnlockedNodes(System.Array.Empty<string>());
        }

        private void OnSkillUnlocked(SkillDefinition skill)
        {
            if (skill == null || player == null)
                return;

            player.LearnSkill(skill);
            skillBar?.Rebuild();
        }

        private void OnNodeRankChanged(SkillTreeNodeDefinition node, int rank)
        {
            if (rank == 0 && node != null && node.UnlockAction == SkillTreeUnlockAction.UnlockSkill &&
                node.SkillToUnlock != null)
            {
                skillLoadout?.RemoveSkill(node.SkillToUnlock.SkillId, false);
                RebuildLearnedSkills();
            }
            else
            {
                ApplyRankEffects();
            }
            ProgressionCommitted?.Invoke();
        }

        private void ApplyRankEffects()
        {
            if (player == null || player.Stats == null || skillTree == null)
                return;

            player.Stats.RemoveModifiers(this);
            player.SkillService?.ClearProgressionModifiers();

            var attackPower = 0f;
            var defense = 0f;
            var attackSpeed = 0f;
            var moveSpeed = 0f;
            var jumpHeight = 0f;
            var skillModifiers = new Dictionary<string, Vector3>();

            foreach (var nodeId in _progress.UnlockedNodeIds)
            {
                if (!skillTree.TryGetNode(nodeId, out var node))
                    continue;

                var rank = _progress.GetRank(nodeId);
                foreach (var effect in node.RankEffects)
                {
                    if (effect == null || !effect.IsValid)
                        continue;

                    var appliedRanks = Mathf.Max(0, rank - effect.FirstAppliedRank + 1);
                    var amount = effect.AmountPerRank * appliedRanks;
                    if (amount == 0f)
                        continue;
                    switch (effect.EffectType)
                    {
                        case SkillTreeRankEffectType.AttackPower:
                            attackPower += amount;
                            break;
                        case SkillTreeRankEffectType.Defense:
                            defense += amount;
                            break;
                        case SkillTreeRankEffectType.AttackSpeed:
                            attackSpeed += amount;
                            break;
                        case SkillTreeRankEffectType.MoveSpeed:
                            moveSpeed += amount;
                            break;
                        case SkillTreeRankEffectType.JumpHeight:
                            jumpHeight += amount;
                            break;
                        default:
                            AddSkillModifier(skillModifiers, effect, amount);
                            break;
                    }
                }
            }

            player.Stats.SetAttackPowerModifier(this, attackPower);
            player.Stats.SetDefenseModifier(this, defense);
            player.Stats.SetAttackSpeedModifier(this, attackSpeed);
            player.Stats.SetMoveSpeedModifier(this, moveSpeed);
            player.Stats.SetJumpHeightModifier(this, jumpHeight);

            foreach (var pair in skillModifiers)
            {
                player.SkillService?.SetProgressionModifiers(pair.Key,
                    new SkillRuntimeModifiers(pair.Value.x, pair.Value.y, pair.Value.z));
            }
        }

        private static void AddSkillModifier(Dictionary<string, Vector3> modifiers,
            SkillTreeRankEffectDefinition effect, float amount)
        {
            if (effect.TargetSkill == null)
                return;

            modifiers.TryGetValue(effect.TargetSkill.SkillId, out var values);
            switch (effect.EffectType)
            {
                case SkillTreeRankEffectType.SkillDamagePercent:
                    values.x += amount;
                    break;
                case SkillTreeRankEffectType.SkillCooldownReductionPercent:
                    values.y += amount;
                    break;
                case SkillTreeRankEffectType.SkillManaCostReductionPercent:
                    values.z += amount;
                    break;
            }

            modifiers[effect.TargetSkill.SkillId] = values;
        }
    }
}

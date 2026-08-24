using System;
using LetterHunter.SkillTree;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LetterHunter.UI.SkillTree
{
    [DisallowMultipleComponent]
    public sealed class SkillTreeNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string configuredNodeId;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button refundButton;
        [SerializeField] private Image background;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text effectText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text upgradeButtonText;
        [SerializeField] private TMP_Text selectButtonText;
        [SerializeField] private TMP_Text refundButtonText;
        [SerializeField] private Color unlockedColor = new(0.15f, 0.45f, 0.22f, 0.95f);
        [SerializeField] private Color availableColor = new(0.18f, 0.25f, 0.42f, 0.95f);
        [SerializeField] private Color lockedColor = new(0.12f, 0.12f, 0.14f, 0.9f);

        private string _nodeId;
        private Action<string> _unlockClicked;
        private Action<string> _selectClicked;
        private Action<string, bool> _hoverChanged;
        private Action<string> _refundClicked;

        public string ConfiguredNodeId => configuredNodeId;

        private void Awake()
        {
            WireButtons();
        }

        private void WireButtons()
        {
            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnSelectClicked);
                selectButton.onClick.AddListener(OnSelectClicked);
            }

            if (refundButton != null)
            {
                refundButton.onClick.RemoveListener(OnRefundClicked);
                refundButton.onClick.AddListener(OnRefundClicked);
            }
        }

        public void Bind(string nodeId, Action<string> unlockClicked, Action<string> selectClicked,
            Action<string, bool> hoverChanged = null, Action<string> refundClicked = null)
        {
            _nodeId = nodeId;
            _unlockClicked = unlockClicked;
            _selectClicked = selectClicked;
            _hoverChanged = hoverChanged;
            _refundClicked = refundClicked;
            WireButtons();
        }

        public void Render(SkillTreeNodeDefinition node, SkillTreeProgress progress, SkillTreeUnlockFailure failure,
            bool refundConfirmation = false)
        {
            var rank = progress != null ? progress.GetRank(node.NodeId) : 0;
            var unlocked = rank > 0;
            var available = failure == SkillTreeUnlockFailure.None;
            var canAssignUnlockedSkill = unlocked &&
                                         node.UnlockAction == SkillTreeUnlockAction.UnlockSkill &&
                                         node.SkillToUnlock != null;

            if (titleText != null)
                titleText.text = node.DisplayName;
            if (typeText != null)
                typeText.text = node.NodeType.ToString().ToUpperInvariant();
            if (iconImage != null)
            {
                iconImage.sprite = node.Icon;
                iconImage.enabled = node.Icon != null;
            }
            if (descriptionText != null)
                descriptionText.text = node.Description;
            if (effectText != null)
                effectText.text = BuildEffectText(node, rank);
            if (costText != null)
                costText.text = BuildCostText(node);
            if (stateText != null)
                stateText.text = failure == SkillTreeUnlockFailure.MaxRankReached ||
                                 failure == SkillTreeUnlockFailure.AlreadyUnlocked
                    ? "Max"
                    : available ? string.Empty
                    : FailureText(failure);
            if (rankText != null)
                rankText.text = $"Rank {rank}/{node.MaxRank}";
            if (background != null)
                background.color = unlocked ? unlockedColor : available ? availableColor : lockedColor;
            var maxed = failure is SkillTreeUnlockFailure.AlreadyUnlocked or
                SkillTreeUnlockFailure.MaxRankReached;
            if (upgradeButton != null)
                upgradeButton.interactable = !maxed;
            if (upgradeButtonText != null)
                upgradeButtonText.text = failure is SkillTreeUnlockFailure.AlreadyUnlocked or
                    SkillTreeUnlockFailure.MaxRankReached ? "Max" : unlocked ? "Upgrade" : "Unlock";
            if (selectButton != null)
            {
                selectButton.gameObject.SetActive(canAssignUnlockedSkill);
                selectButton.interactable = canAssignUnlockedSkill;
            }
            if (selectButtonText != null)
                selectButtonText.text = "Select";
            if (refundButton != null)
            {
                refundButton.gameObject.SetActive(unlocked);
                refundButton.interactable = unlocked;
            }
            if (refundButtonText != null)
                refundButtonText.text = refundConfirmation ? "Confirm" : "Refund";
        }

        private void OnUpgradeClicked()
        {
            if (!string.IsNullOrWhiteSpace(_nodeId))
                _unlockClicked?.Invoke(_nodeId);
        }

        private void OnSelectClicked()
        {
            if (!string.IsNullOrWhiteSpace(_nodeId))
                _selectClicked?.Invoke(_nodeId);
        }

        private void OnRefundClicked()
        {
            if (!string.IsNullOrWhiteSpace(_nodeId))
                _refundClicked?.Invoke(_nodeId);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!string.IsNullOrWhiteSpace(_nodeId))
                _hoverChanged?.Invoke(_nodeId, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!string.IsNullOrWhiteSpace(_nodeId))
                _hoverChanged?.Invoke(_nodeId, false);
        }

        private static string BuildCostText(SkillTreeNodeDefinition node)
        {
            var text = node.GoldCost > 0 ? $"{node.GoldCost} gold" : "Free";
            foreach (var cost in node.MaterialCosts)
            {
                if (!cost.IsValid)
                    continue;
                text += $"  {cost.Item.DisplayName} x{cost.Amount}";
            }

            return text;
        }

        public static string BuildEffectText(SkillTreeNodeDefinition node, int currentRank)
        {
            var text = string.Empty;
            foreach (var effect in node.RankEffects)
            {
                if (effect == null || !effect.IsValid)
                    continue;

                if (text.Length > 0)
                    text += "  ";

                var isPercent = effect.EffectType is SkillTreeRankEffectType.SkillDamagePercent or
                    SkillTreeRankEffectType.SkillCooldownReductionPercent or
                    SkillTreeRankEffectType.SkillManaCostReductionPercent;
                var target = effect.TargetSkill != null ? $" {effect.TargetSkill.ShortName}" : string.Empty;
                var label = effect.EffectType switch
                {
                    SkillTreeRankEffectType.AttackPower => "Attack",
                    SkillTreeRankEffectType.Defense => "Defense",
                    SkillTreeRankEffectType.AttackSpeed => "Attack Speed",
                    SkillTreeRankEffectType.MoveSpeed => "Move Speed",
                    SkillTreeRankEffectType.JumpHeight => "Jump Height",
                    SkillTreeRankEffectType.SkillDamagePercent => $"{target} Damage",
                    SkillTreeRankEffectType.SkillCooldownReductionPercent => $"{target} Cooldown Reduction",
                    SkillTreeRankEffectType.SkillManaCostReductionPercent => $"{target} Mana Reduction",
                    _ => "Upgrade"
                };
                var nextRank = Mathf.Min(node.MaxRank, currentRank + 1);
                var appliedRanks = Mathf.Max(0, currentRank - effect.FirstAppliedRank + 1);
                var nextAppliedRanks = Mathf.Max(0, nextRank - effect.FirstAppliedRank + 1);
                var currentAmount = effect.AmountPerRank * appliedRanks;
                var nextAmount = effect.AmountPerRank * nextAppliedRanks;

                if (currentRank < effect.FirstAppliedRank && nextAppliedRanks == appliedRanks)
                    text += $"{label}: starts at rank {effect.FirstAppliedRank}";
                else if (currentRank >= node.MaxRank)
                    text += $"{label}: +{FormatEffectAmount(currentAmount, isPercent)} (Max)";
                else
                    text += $"{label}: +{FormatEffectAmount(currentAmount, isPercent)} -> +{FormatEffectAmount(nextAmount, isPercent)}";
            }

            return text;
        }

        private static string FormatEffectAmount(float amount, bool isPercent) =>
            isPercent ? $"{amount * 100f:0.#}%" : $"{amount:0.##}";

        private static string FailureText(SkillTreeUnlockFailure failure) => failure switch
        {
            SkillTreeUnlockFailure.MissingPrerequisite => "Requires previous node",
            SkillTreeUnlockFailure.NotEnoughGold => "Need gold",
            SkillTreeUnlockFailure.NotEnoughMaterials => "Need materials",
            SkillTreeUnlockFailure.AlreadyUnlocked => "Unlocked",
            SkillTreeUnlockFailure.MaxRankReached => "Max",
            _ => "Locked"
        };
    }
}

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
        [SerializeField] private SkillNodeDefinitionSO configuredNode;
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image frame;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject purchasedGlow;
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private Color lockedColor = new(0.16f, 0.16f, 0.18f, 0.82f);
        [SerializeField] private Color availableColor = new(0.15f, 0.32f, 0.38f, 1f);
        [SerializeField] private Color insufficientColor = new(0.34f, 0.2f, 0.16f, 0.95f);
        [SerializeField] private Color purchasedColor = new(0.17f, 0.42f, 0.28f, 1f);

        private Action<SkillNodeDefinitionSO> _selected;
        private Action<SkillNodeDefinitionSO, bool> _hovered;
        public SkillNodeDefinitionSO ConfiguredNode => configuredNode;

        private void Awake() => WireButton();

        public void Bind(SkillNodeDefinitionSO node, Action<SkillNodeDefinitionSO> selected,
            Action<SkillNodeDefinitionSO, bool> hovered)
        {
            configuredNode = node;
            _selected = selected;
            _hovered = hovered;
            WireButton();
        }

        public void Render(SkillTreeNodeState state, bool selected)
        {
            if (configuredNode == null) return;
            if (titleText != null) titleText.text = configuredNode.DisplayName;
            if (icon != null)
            {
                icon.sprite = configuredNode.Icon;
                icon.enabled = configuredNode.Icon != null;
                icon.color = state == SkillTreeNodeState.Locked
                    ? new Color(0.38f, 0.4f, 0.42f, 0.72f)
                    : Color.white;
            }
            if (background != null)
                background.color = state switch
                {
                    SkillTreeNodeState.Available => availableColor,
                    SkillTreeNodeState.Purchased => purchasedColor,
                    SkillTreeNodeState.UnavailableByFunds => insufficientColor,
                    _ => lockedColor
                };
            if (stateText != null)
                stateText.text = state switch
                {
                    SkillTreeNodeState.Available => "AVAILABLE",
                    SkillTreeNodeState.Purchased => "PURCHASED",
                    SkillTreeNodeState.UnavailableByFunds => "NEED COINS",
                    _ => $"LEVEL {configuredNode.RequiredLevel}"
                };
            if (lockedOverlay != null) lockedOverlay.SetActive(state == SkillTreeNodeState.Locked);
            if (purchasedGlow != null) purchasedGlow.SetActive(state == SkillTreeNodeState.Purchased);
            if (selectedFrame != null) selectedFrame.SetActive(selected);
            if (frame != null) frame.color = selected ? new Color(1f, 0.66f, 0.19f, 1f) : Color.white;
            if (button != null) button.interactable = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * 1.045f;
            _hovered?.Invoke(configuredNode, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
            _hovered?.Invoke(configuredNode, false);
        }

        private void WireButton()
        {
            if (button == null) button = GetComponent<Button>();
            if (button == null) return;
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            if (configuredNode != null) _selected?.Invoke(configuredNode);
        }
    }
}

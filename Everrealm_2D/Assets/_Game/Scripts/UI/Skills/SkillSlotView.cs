using System;
using LetterHunter.Items;
using LetterHunter.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LetterHunter.UI.Skills
{
    [DisallowMultipleComponent]
    public sealed class SkillSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private TMP_Text keyText;
        [SerializeField] private TMP_Text nameText;

        [Header("Colors")]
        [SerializeField] private Color readyColor = Color.white;
        [SerializeField] private Color notEnoughManaColor = new(0.35f, 0.45f, 1f, 0.75f);
        [SerializeField] private Color emptyColor = new(0.12f, 0.12f, 0.12f, 0.85f);
        [SerializeField] private Color assignmentColor = new(0.95f, 0.78f, 0.25f, 0.95f);

        private int _index;
        private bool _hasSkill;
        private bool _hasConsumable;
        private bool _dragHidden;

        public event Action<int> Clicked;
        public event Action<int> PointerEntered;
        public event Action<int> PointerExited;
        public event Action<int, PointerEventData> DragBegan;
        public event Action<PointerEventData> DragMoved;
        public event Action DragEnded;
        public event Action<int> Dropped;
        public event Action<int, InventorySlotView> ItemDropped;

        public void DisableTemplateIconLayers()
        {
            foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            {
                if (graphic == null || graphic == icon)
                    continue;

                var layerName = graphic.gameObject.name;
                if (layerName.Equals("Icon", StringComparison.OrdinalIgnoreCase) ||
                    layerName.Equals("IconGlow", StringComparison.OrdinalIgnoreCase))
                    graphic.enabled = false;
            }
        }

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
                button.onClick.AddListener(OnClicked);
            }
        }

        public void Render(SkillSlotViewModel model)
        {
            _index = model.Index;
            _hasSkill = model.HasSkill;
            _hasConsumable = false;
            var showSkill = model.HasSkill && !_dragHidden;

            if (keyText != null) keyText.text = model.InputLabel;
            if (nameText != null) nameText.text = showSkill ? model.Skill.ShortName : "Empty";

            if (icon != null)
            {
                icon.enabled = showSkill && model.Skill.Icon != null;
                icon.sprite = showSkill ? model.Skill.Icon : null;
                icon.color = showSkill
                    ? (model.CanAffordMana ? readyColor : notEnoughManaColor)
                    : Color.clear;
            }

            if (background != null)
                background.color = showSkill ? Color.white : emptyColor;

            if (cooldownOverlay != null)
            {
                cooldownOverlay.enabled = showSkill && !model.IsReady;
                cooldownOverlay.fillAmount = model.CooldownFill;
            }

            if (cooldownText != null)
            {
                cooldownText.enabled = showSkill && !model.IsReady;
                cooldownText.text = showSkill && model.CooldownRemaining > 0f
                    ? Mathf.CeilToInt(model.CooldownRemaining).ToString()
                    : string.Empty;
            }

            if (button != null)
                button.interactable = showSkill && model.IsReady;
        }

        public void RenderConsumable(ItemDefinition item, int amount, string inputLabel)
        {
            _hasSkill = false;
            _hasConsumable = item != null && amount > 0;

            if (keyText != null)
                keyText.text = inputLabel ?? string.Empty;
            if (nameText != null)
                nameText.text = _hasConsumable ? $"{item.DisplayName} x{amount}" : "Empty";
            if (icon != null)
            {
                icon.enabled = _hasConsumable && item.Icon != null;
                icon.sprite = _hasConsumable ? item.Icon : null;
                icon.color = Color.white;
            }
            if (background != null)
                background.color = _hasConsumable ? Color.white : emptyColor;
            if (cooldownOverlay != null)
                cooldownOverlay.enabled = false;
            if (cooldownText != null)
            {
                cooldownText.enabled = false;
                cooldownText.text = string.Empty;
            }
            if (button != null)
                button.interactable = _hasConsumable;
        }

        public void SetDragHidden(bool hidden)
        {
            _dragHidden = hidden && _hasSkill;
        }

        public void SetAssignmentTarget(bool assignmentMode)
        {
            if (button != null)
                button.interactable = assignmentMode || button.interactable;
            if (!assignmentMode)
                return;

            if (background != null)
                background.color = assignmentColor;
            if (nameText != null && (string.IsNullOrWhiteSpace(nameText.text) || nameText.text == "Empty"))
                nameText.text = "Select";
        }

        private void OnClicked()
        {
            Clicked?.Invoke(_index);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PointerEntered?.Invoke(_index);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PointerExited?.Invoke(_index);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_hasSkill && eventData.button == PointerEventData.InputButton.Left)
                DragBegan?.Invoke(_index, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragHidden)
                DragMoved?.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragHidden)
                DragEnded?.Invoke();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            var inventorySlot = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<InventorySlotView>()
                : null;
            if (inventorySlot != null)
                ItemDropped?.Invoke(_index, inventorySlot);
            else
                Dropped?.Invoke(_index);
        }
    }
}

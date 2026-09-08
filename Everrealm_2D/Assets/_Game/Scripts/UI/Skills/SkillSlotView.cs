using System;
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
        private bool _dragHidden;

        public event Action<int> Clicked;
        public event Action<int> PointerEntered;
        public event Action<int> PointerExited;
        public event Action<int, PointerEventData> DragBegan;
        public event Action<PointerEventData> DragMoved;
        public event Action DragEnded;
        public event Action<int> Dropped;

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
            if (eventData.button == PointerEventData.InputButton.Left)
                Dropped?.Invoke(_index);
        }
    }
}

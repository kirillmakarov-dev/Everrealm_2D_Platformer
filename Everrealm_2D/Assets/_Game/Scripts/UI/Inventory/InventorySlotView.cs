using LetterHunter.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LetterHunter.UI.Inventory
{
    [DisallowMultipleComponent]
    public sealed class InventorySlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Image highlight;
        [SerializeField] private Color emptyColor = new(0.08f, 0.09f, 0.11f, 0.92f);
        [SerializeField] private Color filledColor = new(0.16f, 0.18f, 0.22f, 0.96f);

        private InventoryWindowPresenter _presenter;
        private int _index;
        private bool _hasItem;
        private bool _dragHidden;

        public Sprite IconSprite => icon != null && icon.enabled ? icon.sprite : null;
        public ItemDefinition Item { get; private set; }

        public void Bind(InventoryWindowPresenter presenter, int index)
        {
            _presenter = presenter;
            _index = index;
        }

        public void Render(InventorySlot slot)
        {
            _hasItem = slot != null && !slot.IsEmpty;
            Item = _hasItem ? slot.Item : null;
            if (!_hasItem)
                _dragHidden = false;
            var showItem = _hasItem && !_dragHidden;

            if (background != null)
                background.color = showItem ? filledColor : emptyColor;

            if (icon != null)
            {
                icon.enabled = showItem && slot.Item.Icon != null;
                icon.sprite = showItem ? slot.Item.Icon : null;
            }

            if (amountText != null)
            {
                amountText.enabled = showItem && slot.Amount > 1;
                amountText.text = showItem && slot.Amount > 1 ? slot.Amount.ToString() : string.Empty;
            }

            if (highlight != null)
                highlight.enabled = false;
        }

        public void SetDragHidden(bool hidden)
        {
            if (!_hasItem)
                return;

            _dragHidden = hidden;
            if (background != null)
                background.color = hidden ? emptyColor : filledColor;
            if (icon != null)
                icon.enabled = !hidden && icon.sprite != null;
            if (amountText != null)
                amountText.enabled = !hidden && !string.IsNullOrEmpty(amountText.text);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_hasItem)
                _presenter?.BeginSlotDrag(_index, this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _presenter?.UpdateSlotDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _presenter?.EndSlotDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            _presenter?.DropDraggedSlotOn(_index);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
                _presenter?.TrySplitHalfIntoEmptySlot(_index);
        }
    }
}

using System.Collections.Generic;
using LetterHunter.Economy;
using LetterHunter.Items;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LetterHunter.UI.Inventory
{
    [DisallowMultipleComponent]
    public sealed class InventoryWindowPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private CanvasGroup windowGroup;
        [SerializeField] private Transform slotRoot;
        [SerializeField] private List<InventorySlotView> slotViews = new();
        [SerializeField] private Image dragGhost;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private Key toggleKey = Key.B;
        [SerializeField] private bool startVisible;

        private Image _dragGhost;
        private int _dragSourceIndex = -1;
        private InventorySlotView _dragSourceView;
        private Canvas _rootCanvas;

        private void Awake()
        {
            if (inventory == null)
                inventory = FindFirstObjectByType<PlayerInventory>();
            if (wallet == null)
                wallet = FindFirstObjectByType<CurrencyWallet>();
            if (windowGroup == null)
                windowGroup = GetComponent<CanvasGroup>();
            if (_rootCanvas == null)
                _rootCanvas = GetComponentInParent<Canvas>();

            if (dragGhost != null)
            {
                dragGhost.enabled = false;
                dragGhost.raycastTarget = false;
            }

            for (var i = 0; i < slotViews.Count; i++)
                slotViews[i]?.Bind(this, i);

            SetVisible(startVisible);
        }

        private void OnEnable()
        {
            if (inventory != null)
                inventory.InventoryChanged += Render;
            if (wallet != null)
                wallet.GoldChanged += RenderGold;

            if (IsVisible)
                Render();
            else
                RenderGold(wallet != null ? wallet.Gold : 0);
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.InventoryChanged -= Render;
            if (wallet != null)
                wallet.GoldChanged -= RenderGold;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey]?.wasPressedThisFrame == true)
                SetVisible(windowGroup == null || windowGroup.alpha <= 0.01f);
        }

        public void Close() => SetVisible(false);

        public void BeginSlotDrag(int index, InventorySlotView sourceView, UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (inventory == null || inventory.Inventory == null || !inventory.Inventory.IsValidIndex(index))
                return;

            var slot = inventory.Inventory.Slots[index];
            if (slot.IsEmpty || sourceView.IconSprite == null)
                return;

            var draggedSprite = sourceView.IconSprite;
            _dragSourceIndex = index;
            _dragSourceView = sourceView;
            _dragSourceView.SetDragHidden(true);
            CreateDragGhost(draggedSprite);
            UpdateSlotDrag(eventData);
        }

        public void UpdateSlotDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_dragGhost == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragGhost.rectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);
            _dragGhost.rectTransform.anchoredPosition = localPoint;
        }

        public void DropDraggedSlotOn(int targetIndex)
        {
            if (_dragSourceIndex >= 0 && targetIndex >= 0 && targetIndex != _dragSourceIndex)
                inventory?.TryMoveSlot(_dragSourceIndex, targetIndex);
        }

        public void EndSlotDrag()
        {
            _dragSourceView?.SetDragHidden(false);
            _dragSourceIndex = -1;
            _dragSourceView = null;
            if (_dragGhost != null)
            {
                _dragGhost.enabled = false;
                _dragGhost.sprite = null;
            }
            _dragGhost = null;
            if (IsVisible)
                Render();
        }

        public void TrySplitHalfIntoEmptySlot(int sourceIndex)
        {
            if (inventory == null || inventory.Inventory == null || !inventory.Inventory.IsValidIndex(sourceIndex))
                return;

            var source = inventory.Inventory.Slots[sourceIndex];
            if (source.IsEmpty || source.Amount <= 1)
                return;

            var targetIndex = FindFirstEmptySlotIndex();
            if (targetIndex < 0)
                return;

            inventory.TrySplitStack(sourceIndex, targetIndex, source.Amount / 2);
        }

        private void Render()
        {
            if (!IsVisible)
            {
                RenderGold(wallet != null ? wallet.Gold : 0);
                return;
            }

            if (inventory == null || inventory.Inventory == null)
                return;

            var slots = inventory.Inventory.Slots;
            for (var i = 0; i < slotViews.Count; i++)
                slotViews[i]?.Render(i < slots.Count ? slots[i] : null);

            if (capacityText != null)
                capacityText.text = $"{UsedSlots()}/{inventory.Inventory.Capacity}";

            RenderGold(wallet != null ? wallet.Gold : 0);
        }

        private void RenderGold(int gold)
        {
            if (goldText != null)
                goldText.text = gold.ToString();
        }

        private void SetVisible(bool visible)
        {
            if (windowGroup == null)
            {
                gameObject.SetActive(visible);
                if (visible)
                    Render();
                return;
            }

            windowGroup.alpha = visible ? 1f : 0f;
            windowGroup.interactable = visible;
            windowGroup.blocksRaycasts = visible;
            if (visible)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            if (visible)
                Render();
        }

        private bool IsVisible => windowGroup == null ? gameObject.activeSelf : windowGroup.alpha > 0.01f;

        private int FindFirstEmptySlotIndex()
        {
            if (inventory == null || inventory.Inventory == null)
                return -1;

            var slots = inventory.Inventory.Slots;
            for (var i = 0; i < slots.Count; i++)
                if (slots[i].IsEmpty)
                    return i;

            return -1;
        }

        private int UsedSlots()
        {
            if (inventory == null || inventory.Inventory == null)
                return 0;

            var used = 0;
            foreach (var slot in inventory.Inventory.Slots)
                if (!slot.IsEmpty)
                    used++;
            return used;
        }

        private void CreateDragGhost(Sprite sprite)
        {
            if (dragGhost == null)
                return;

            _dragGhost = dragGhost;
            _dragGhost.sprite = sprite;
            _dragGhost.raycastTarget = false;
            _dragGhost.preserveAspect = true;
            _dragGhost.enabled = true;

            var group = _dragGhost.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.blocksRaycasts = false;
                group.alpha = 0.88f;
            }
        }
    }
}

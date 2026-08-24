using System.Collections.Generic;
using LetterHunter.Economy;
using LetterHunter.Items;
using LetterHunter.Save;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LetterHunter.UI.Shop
{
    [DisallowMultipleComponent]
    public sealed class ShopWindowPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private PlayerSaveController saveController;
        [SerializeField] private CanvasGroup windowGroup;
        [SerializeField] private RectTransform rowRoot;
        [SerializeField] private ShopItemRowView rowPrefab;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Key toggleKey = Key.O;
        [SerializeField] private bool startVisible;

        private readonly List<ShopItemRowView> _rows = new();
        private ShopService _shop;

        private void Awake()
        {
            if (inventory == null)
                inventory = FindFirstObjectByType<PlayerInventory>();
            if (wallet == null)
                wallet = FindFirstObjectByType<CurrencyWallet>();
            if (saveController == null)
                saveController = FindFirstObjectByType<PlayerSaveController>();
            if (windowGroup == null)
                windowGroup = GetComponent<CanvasGroup>();

            _shop = new ShopService(inventory != null ? inventory.RuntimeInventory : null, wallet);
            SetVisible(startVisible);
        }

        private void OnEnable()
        {
            if (inventory != null)
                inventory.InventoryChanged += Render;
            if (wallet != null)
                wallet.GoldChanged += RenderGold;

            Render();
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
                SetVisible(!IsVisible);
        }

        public void SetVisible(bool visible)
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
                Render();
            }
        }

        private void Render()
        {
            RenderGold(wallet != null ? wallet.Gold : 0);
            if (!IsVisible || rowRoot == null)
                return;

            ClearRows();
            var sellableItems = BuildSellableItemCounts();
            if (sellableItems.Count == 0)
            {
                SetEmptyState(true);
                return;
            }

            SetEmptyState(false);
            foreach (var pair in sellableItems)
                CreateSellRow(pair.Key, pair.Value);
        }

        private void RenderGold(int gold)
        {
            if (goldText != null)
                goldText.text = gold.ToString();
        }

        private Dictionary<ItemDefinition, int> BuildSellableItemCounts()
        {
            var result = new Dictionary<ItemDefinition, int>();
            if (inventory == null || inventory.Inventory == null)
                return result;

            foreach (var slot in inventory.Inventory.Slots)
            {
                if (slot.IsEmpty || slot.Item == null || !slot.Item.CanSell)
                    continue;

                result.TryAdd(slot.Item, 0);
                result[slot.Item] += slot.Amount;
            }

            return result;
        }

        private void CreateSellRow(ItemDefinition item, int amount)
        {
            if (rowPrefab == null || rowRoot == null)
                return;

            var row = Instantiate(rowPrefab, rowRoot);
            row.name = $"{item.DisplayName}Row";
            row.Render(item, amount, Sell);
            _rows.Add(row);
        }

        private void Sell(ItemDefinition item, int amount)
        {
            var result = _shop.TrySell(item, amount);
            if (feedbackText != null)
                feedbackText.text = result.Success
                    ? $"+{result.GoldEarned} gold"
                    : SellFailureToText(result.Failure);

            if (result.Success)
                saveController?.SaveToDisk();

            Render();
        }

        private static string SellFailureToText(SellFailureReason failure) => failure switch
        {
            SellFailureReason.InvalidItem => "Invalid item",
            SellFailureReason.InvalidAmount => "Invalid amount",
            SellFailureReason.CannotSell => "Cannot sell",
            SellFailureReason.NotEnoughItems => "Not enough items",
            _ => "Could not sell"
        };

        private void ClearRows()
        {
            foreach (var row in _rows)
                if (row != null)
                    Destroy(row.gameObject);
            _rows.Clear();
        }

        private void SetEmptyState(bool isEmpty)
        {
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(isEmpty);
                if (isEmpty)
                    emptyText.text = "No sellable items";
            }
        }

        private bool IsVisible => windowGroup == null ? gameObject.activeSelf : windowGroup.alpha > 0.01f;
    }
}

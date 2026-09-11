using System.Collections.Generic;
using LetterHunter.Economy;
using LetterHunter.Characters;
using LetterHunter.Items;
using LetterHunter.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.UI.Shop
{
    [DisallowMultipleComponent]
    public sealed class ShopWindowPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private PlayerSaveController saveController;
        [SerializeField] private CharacterInputRouter characterInput;
        [SerializeField] private CanvasGroup windowGroup;
        [SerializeField] private RectTransform rowRoot;
        [SerializeField] private ShopItemRowView rowPrefab;
        [SerializeField] private List<ShopItemRowView> authoredRows = new();
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button buyTabButton;
        [SerializeField] private Button sellTabButton;
        [SerializeField] private bool startVisible;

        private readonly List<ShopItemRowView> _rows = new();
        private ShopInteraction2D _activeShop;
        private ShopService _shop;
        private ShopCatalogDefinition _catalog;
        private bool _buyMode = true;

        private void Awake()
        {
            if (inventory == null)
                inventory = FindFirstObjectByType<PlayerInventory>();
            if (wallet == null)
                wallet = FindFirstObjectByType<CurrencyWallet>();
            if (saveController == null)
                saveController = FindFirstObjectByType<PlayerSaveController>();
            if (characterInput == null)
                characterInput = FindFirstObjectByType<CharacterInputRouter>();
            if (windowGroup == null)
                windowGroup = GetComponent<CanvasGroup>();

            buyTabButton?.onClick.AddListener(ShowBuyTab);
            sellTabButton?.onClick.AddListener(ShowSellTab);
            SetVisible(startVisible);
        }

        private void OnDestroy()
        {
            buyTabButton?.onClick.RemoveListener(ShowBuyTab);
            sellTabButton?.onClick.RemoveListener(ShowSellTab);
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
            characterInput?.SetBlocked(false);
        }

        public bool IsVisible => windowGroup == null ? gameObject.activeSelf : windowGroup.alpha > 0.01f;

        public event System.Action<bool> VisibilityChanged;

        public void SetVisible(bool visible)
        {
            characterInput?.SetBlocked(visible);
            if (windowGroup == null)
            {
                gameObject.SetActive(visible);
                if (visible)
                    Render();
                VisibilityChanged?.Invoke(visible);
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

            VisibilityChanged?.Invoke(visible);
        }

        public void CloseShop()
        {
            SetVisible(false);
        }

        public void ShowBuyTab()
        {
            _buyMode = true;
            Render();
        }

        public void ShowSellTab()
        {
            _buyMode = false;
            Render();
        }

        public void SetShop(ShopInteraction2D shop)
        {
            _activeShop = shop;
            _catalog = shop != null ? shop.Catalog : null;
            _shop = shop != null ? shop.Service : null;
            Render();
        }

        private void Render()
        {
            RenderGold(wallet != null ? wallet.Gold : 0);
            SetTabState();
            if (!IsVisible || rowRoot == null)
                return;

            ClearRows();
            if (_buyMode)
            {
                RenderBuyRows();
                return;
            }

            RenderSellRows();
        }

        private void RenderBuyRows()
        {
            if (_catalog == null || _catalog.Entries.Count == 0)
            {
                SetEmptyState(true, "No items available");
                return;
            }

            SetEmptyState(false, string.Empty);
            var rowIndex = 0;
            foreach (var entry in _catalog.Entries)
            {
                if (rowIndex >= authoredRows.Count)
                    break;

                if (entry == null || entry.Item == null)
                    continue;

                var row = authoredRows[rowIndex++];
                if (row == null)
                    continue;

                row.gameObject.SetActive(true);
                row.RenderBuy(entry.Item, entry.BuyPrice, Buy);
                _rows.Add(row);
            }

            HideUnusedRows(rowIndex);
        }

        private void RenderSellRows()
        {
            var sellableItems = BuildSellableItemCounts();
            if (sellableItems.Count == 0)
            {
                SetEmptyState(true, "No sellable items");
                return;
            }

            SetEmptyState(false, string.Empty);
            var rowIndex = 0;
            foreach (var pair in sellableItems)
            {
                if (rowIndex >= authoredRows.Count)
                    break;

                var row = authoredRows[rowIndex++];
                if (row == null)
                    continue;

                row.gameObject.SetActive(true);
                row.Render(pair.Key, pair.Value, Sell);
                _rows.Add(row);
            }

            HideUnusedRows(rowIndex);
        }

        private void HideUnusedRows(int rowIndex)
        {
            for (; rowIndex < authoredRows.Count; rowIndex++)
                if (authoredRows[rowIndex] != null)
                    authoredRows[rowIndex].gameObject.SetActive(false);
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
                if (slot.IsEmpty || slot.Item == null || !slot.Item.CanSell ||
                    (_catalog != null && !_catalog.CanSell(slot.Item)))
                    continue;

                result.TryAdd(slot.Item, 0);
                result[slot.Item] += slot.Amount;
            }

            return result;
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

        private void Buy(ItemDefinition item, int amount)
        {
            var result = _shop.TryBuy(item, amount);
            if (feedbackText != null)
                feedbackText.text = result.Success
                    ? $"-{result.GoldSpent} gold"
                    : BuyFailureToText(result.Failure);

            if (result.Success)
                saveController?.SaveToDisk();

            Render();
        }

        private void SetTabState()
        {
            if (buyTabButton != null)
                SetTabAppearance(buyTabButton, _buyMode);
            if (sellTabButton != null)
                SetTabAppearance(sellTabButton, !_buyMode);
        }

        private static void SetTabAppearance(Button button, bool active)
        {
            var colors = button.colors;
            colors.normalColor = active
                ? new Color(0.86f, 0.58f, 0.2f, 1f)
                : new Color(0.25f, 0.22f, 0.18f, 1f);
            colors.highlightedColor = active
                ? new Color(1f, 0.75f, 0.35f, 1f)
                : new Color(0.42f, 0.36f, 0.27f, 1f);
            colors.pressedColor = new Color(0.72f, 0.44f, 0.14f, 1f);
            button.colors = colors;
            button.interactable = true;
        }

        private static string BuyFailureToText(BuyFailureReason failure) => failure switch
        {
            BuyFailureReason.InvalidItem => "Invalid item",
            BuyFailureReason.InvalidAmount => "Invalid amount",
            BuyFailureReason.NotInCatalog => "Item is not sold here",
            BuyFailureReason.NotEnoughGold => "Not enough gold",
            BuyFailureReason.InventoryFull => "Inventory is full",
            _ => "Could not buy"
        };

        private static string SellFailureToText(SellFailureReason failure) => failure switch
        {
            SellFailureReason.InvalidItem => "Invalid item",
            SellFailureReason.InvalidAmount => "Invalid amount",
            SellFailureReason.CannotSell => "Cannot sell",
            SellFailureReason.NotEnoughItems => "Not enough items",
            SellFailureReason.NotInCatalog => "Item is not accepted here",
            _ => "Could not sell"
        };

        private void ClearRows()
        {
            foreach (var row in _rows)
                if (row != null)
                    row.gameObject.SetActive(false);
            _rows.Clear();
        }

        private void SetEmptyState(bool isEmpty, string message)
        {
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(isEmpty);
                if (isEmpty)
                    emptyText.text = message;
            }
        }

    }
}

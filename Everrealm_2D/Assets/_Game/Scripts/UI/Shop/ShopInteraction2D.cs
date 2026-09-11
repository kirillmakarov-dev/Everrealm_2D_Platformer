using LetterHunter.Characters;
using LetterHunter.Economy;
using LetterHunter.Items;
using LetterHunter.UI.Inventory;
using TMPro;
using UnityEngine;

namespace LetterHunter.UI.Shop
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class ShopInteraction2D : MonoBehaviour
    {
        [SerializeField] private ShopWindowPresenter shopWindow;
        [SerializeField] private ShopCatalogDefinition shopCatalog;
        [SerializeField] private InventoryWindowPresenter inventoryWindow;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private CanvasGroup interactionPrompt;
        [SerializeField] private TMP_Text interactionPromptText;

        private CharacterInputRouter _playerInput;
        private ShopService _shopService;
        private bool _playerInRange;

        public ShopCatalogDefinition Catalog => shopCatalog;
        public ShopService Service => _shopService;

        private void Awake()
        {
            var trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
            if (shopWindow == null)
                shopWindow = FindFirstObjectByType<ShopWindowPresenter>(FindObjectsInactive.Include);
            if (inventoryWindow == null)
                inventoryWindow = FindFirstObjectByType<InventoryWindowPresenter>(FindObjectsInactive.Include);
            if (playerInventory == null)
                playerInventory = FindFirstObjectByType<PlayerInventory>();
            if (wallet == null)
                wallet = FindFirstObjectByType<CurrencyWallet>();
            RebuildShopService();
            if (shopWindow != null)
            {
                shopWindow.VisibilityChanged += OnShopVisibilityChanged;
            }
            SetPromptVisible(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var input = other.GetComponentInParent<CharacterInputRouter>();
            if (input == null)
                return;

            if (_playerInput == input)
                return;

            _playerInput = input;
            _playerInRange = true;
            _playerInput.InteractRequested += ToggleShop;
            UpdatePromptText();
            SetPromptVisible(shopWindow == null || !shopWindow.IsVisible);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var input = other.GetComponentInParent<CharacterInputRouter>();
            if (input == null || input != _playerInput)
                return;

            _playerInRange = false;
            _playerInput.InteractRequested -= ToggleShop;
            _playerInput = null;
            SetPromptVisible(false);
        }

        private void OnDisable()
        {
            if (shopWindow != null)
                shopWindow.VisibilityChanged -= OnShopVisibilityChanged;
            if (_playerInput != null)
                _playerInput.InteractRequested -= ToggleShop;
            _playerInput = null;
            _playerInRange = false;
            SetPromptVisible(false);
        }

        private void ToggleShop()
        {
            if (!_playerInRange || shopWindow == null)
                return;

            var shouldOpen = !shopWindow.IsVisible;
            if (shouldOpen)
            {
                RebuildShopService();
                shopWindow.SetShop(this);
            }
            shopWindow.SetVisible(shouldOpen);
            inventoryWindow?.SetVisible(shouldOpen);
            SetPromptVisible(!shouldOpen);
        }

        private void OnShopVisibilityChanged(bool visible)
        {
            if (!visible)
                inventoryWindow?.SetVisible(false);
            SetPromptVisible(_playerInRange && !visible);
        }

        private void RebuildShopService()
        {
            _shopService = new ShopService(
                playerInventory != null ? playerInventory.RuntimeInventory : null,
                wallet,
                shopCatalog);
        }

        private void UpdatePromptText()
        {
            if (_playerInput == null || interactionPromptText == null)
                return;

            interactionPromptText.text = $"Press {_playerInput.InteractKey} to interact";
        }

        private void SetPromptVisible(bool visible)
        {
            if (interactionPrompt == null)
                return;

            interactionPrompt.alpha = visible ? 1f : 0f;
            interactionPrompt.interactable = false;
            interactionPrompt.blocksRaycasts = false;
        }
    }
}

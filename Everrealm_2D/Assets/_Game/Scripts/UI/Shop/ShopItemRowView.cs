using System;
using LetterHunter.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.UI.Shop
{
    [DisallowMultipleComponent]
    public sealed class ShopItemRowView : MonoBehaviour
    {
        [SerializeField] private Image itemIcon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Button sellOneButton;
        [SerializeField] private Button sellStackButton;

        private ItemDefinition _item;
        private int _amount;
        private int _price;
        private Action<ItemDefinition, int> _actionRequested;

        private void Awake()
        {
            if (sellOneButton != null)
                sellOneButton.onClick.AddListener(SellOne);
            if (sellStackButton != null)
                sellStackButton.onClick.AddListener(SellStack);
        }

        public void Render(ItemDefinition item, int amount, Action<ItemDefinition, int> actionRequested)
        {
            _item = item;
            _amount = amount;
            _price = item != null ? item.SellPrice : 0;
            _actionRequested = actionRequested;

            if (itemIcon != null)
            {
                itemIcon.sprite = item != null ? item.Icon : null;
                itemIcon.enabled = item != null && item.Icon != null;
                itemIcon.preserveAspect = true;
            }

            if (nameText != null)
                nameText.text = item != null ? item.DisplayName : string.Empty;
            if (priceText != null)
                priceText.text = item != null ? $"{item.SellPrice}g" : string.Empty;
            if (amountText != null)
                amountText.text = amount > 1 ? $"x{amount}" : "x1";
            if (sellOneButton != null)
                sellOneButton.interactable = item != null && amount > 0;
            if (sellStackButton != null)
                sellStackButton.interactable = item != null && amount > 1;
            SetButtonLabel(sellOneButton, "Sell 1");
            SetButtonLabel(sellStackButton, "Sell all");
        }

        public void RenderBuy(ItemDefinition item, int price, Action<ItemDefinition, int> buyRequested)
        {
            _item = item;
            _amount = 5;
            _price = Mathf.Max(1, price);
            _actionRequested = buyRequested;

            if (itemIcon != null)
            {
                itemIcon.sprite = item != null ? item.Icon : null;
                itemIcon.enabled = item != null && item.Icon != null;
                itemIcon.preserveAspect = true;
            }
            if (nameText != null)
                nameText.text = item != null ? item.DisplayName : string.Empty;
            if (priceText != null)
                priceText.text = item != null ? $"{_price}g" : string.Empty;
            if (amountText != null)
                amountText.text = item != null ? "Buy 1 / 5" : string.Empty;
            if (sellOneButton != null)
                sellOneButton.interactable = item != null;
            if (sellStackButton != null)
                sellStackButton.interactable = item != null;
            SetButtonLabel(sellOneButton, "Buy 1");
            SetButtonLabel(sellStackButton, "Buy 5");
        }

        private void SellOne()
        {
            if (_item != null)
                _actionRequested?.Invoke(_item, 1);
        }

        private void SellStack()
        {
            if (_item != null)
                _actionRequested?.Invoke(_item, _amount);
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
                return;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = text;
        }
    }
}

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
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Button sellOneButton;
        [SerializeField] private Button sellStackButton;

        private ItemDefinition _item;
        private int _amount;
        private Action<ItemDefinition, int> _sellRequested;

        private void Awake()
        {
            if (sellOneButton != null)
                sellOneButton.onClick.AddListener(SellOne);
            if (sellStackButton != null)
                sellStackButton.onClick.AddListener(SellStack);
        }

        public void Render(ItemDefinition item, int amount, Action<ItemDefinition, int> sellRequested)
        {
            _item = item;
            _amount = amount;
            _sellRequested = sellRequested;

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
        }

        private void SellOne()
        {
            if (_item != null)
                _sellRequested?.Invoke(_item, 1);
        }

        private void SellStack()
        {
            if (_item != null)
                _sellRequested?.Invoke(_item, _amount);
        }
    }
}

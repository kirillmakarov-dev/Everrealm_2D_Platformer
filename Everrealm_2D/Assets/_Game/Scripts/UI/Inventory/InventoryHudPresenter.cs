using System.Text;
using LetterHunter.Items;
using TMPro;
using UnityEngine;

namespace LetterHunter.UI.Inventory
{
    [DisallowMultipleComponent]
    public sealed class InventoryHudPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text inventoryText;
        [SerializeField] private int maxVisibleSlots = 8;

        private readonly StringBuilder _builder = new();

        private void Awake()
        {
            if (inventory == null)
                inventory = FindFirstObjectByType<PlayerInventory>();
        }

        private void OnEnable()
        {
            if (inventory != null)
                inventory.InventoryChanged += Render;

            Render();
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.InventoryChanged -= Render;
        }

        public void Render()
        {
            if (titleText != null)
                titleText.text = "Inventory";
            if (inventoryText == null)
                return;

            if (inventory == null || inventory.Inventory == null)
            {
                inventoryText.text = "No inventory";
                return;
            }

            _builder.Clear();
            var shown = 0;
            foreach (var slot in inventory.Inventory.Slots)
            {
                if (slot.IsEmpty)
                    continue;

                _builder.Append(slot.Item.DisplayName);
                _builder.Append(" x");
                _builder.Append(slot.Amount);
                _builder.AppendLine();
                shown++;

                if (shown >= maxVisibleSlots)
                    break;
            }

            inventoryText.text = shown > 0 ? _builder.ToString() : "Empty";
        }
    }
}

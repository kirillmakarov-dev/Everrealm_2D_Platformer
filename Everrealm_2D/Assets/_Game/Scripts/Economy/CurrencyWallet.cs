using System;
using UnityEngine;

namespace LetterHunter.Economy
{
    [DisallowMultipleComponent]
    public sealed class CurrencyWallet : MonoBehaviour
    {
        [Min(0), SerializeField] private int startingGold;

        public event Action<int> GoldChanged;
        public int Gold { get; private set; }
        public int StartingGold => Mathf.Max(0, startingGold);

        private void Awake()
        {
            Gold = Mathf.Max(0, startingGold);
        }

        public void AddGold(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (amount == 0)
                return;

            Gold += amount;
            GoldChanged?.Invoke(Gold);
        }

        public bool CanSpendGold(int amount) => amount >= 0 && Gold >= amount;

        public bool TrySpendGold(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (!CanSpendGold(amount))
                return false;

            Gold -= amount;
            GoldChanged?.Invoke(Gold);
            return true;
        }

        public void SetGold(int amount)
        {
            Gold = Mathf.Max(0, amount);
            GoldChanged?.Invoke(Gold);
        }

        public void ResetToStartingGold()
        {
            SetGold(StartingGold);
        }
    }
}

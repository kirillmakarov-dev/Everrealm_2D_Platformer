using LetterHunter.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.UI.Inventory
{
    [DisallowMultipleComponent]
    public sealed class CurrencyHudPresenter : MonoBehaviour
    {
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private Image coinIcon;

        private CurrencyWallet _subscribedWallet;

        private void Awake()
        {
            EnsureWallet();
        }

        private void OnEnable()
        {
            EnsureWallet();
            SubscribeToWallet();
            Render(wallet != null ? wallet.Gold : 0);
        }

        private void Start()
        {
            EnsureWallet();
            SubscribeToWallet();
            Render(wallet != null ? wallet.Gold : 0);
        }

        private void OnDisable()
        {
            UnsubscribeFromWallet();
        }

        public void Render(int gold)
        {
            if (goldText != null)
                goldText.text = gold.ToString();
            if (coinIcon != null)
                coinIcon.enabled = true;
        }

        private void EnsureWallet()
        {
            if (wallet == null)
                wallet = FindFirstObjectByType<CurrencyWallet>();
        }

        private void SubscribeToWallet()
        {
            if (wallet == null || _subscribedWallet == wallet)
                return;

            UnsubscribeFromWallet();
            wallet.GoldChanged += Render;
            _subscribedWallet = wallet;
        }

        private void UnsubscribeFromWallet()
        {
            if (_subscribedWallet == null)
                return;

            _subscribedWallet.GoldChanged -= Render;
            _subscribedWallet = null;
        }
    }
}

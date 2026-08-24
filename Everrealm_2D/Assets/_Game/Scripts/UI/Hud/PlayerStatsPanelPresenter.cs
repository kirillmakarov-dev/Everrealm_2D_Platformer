using LetterHunter.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LetterHunter.UI.Hud
{
    [DisallowMultipleComponent]
    public sealed class PlayerStatsPanelPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerClassController player;
        [SerializeField] private CharacterRoot characterRoot;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text attackText;
        [SerializeField] private TMP_Text defenseText;
        [SerializeField] private TMP_Text moveSpeedText;
        [SerializeField] private TMP_Text attackSpeedText;
        [SerializeField] private TMP_Text jumpHeightText;
        [SerializeField] private Key toggleKey = Key.P;
        [SerializeField] private bool startVisible = true;

        private void Awake()
        {
            if (player == null)
                player = FindFirstObjectByType<PlayerClassController>();
            if (characterRoot == null && player != null)
                characterRoot = player.GetComponent<CharacterRoot>();
            if (panelGroup == null)
                panelGroup = GetComponent<CanvasGroup>();
            SetVisible(startVisible);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey]?.wasPressedThisFrame == true)
                SetVisible(!IsVisible);
            if (IsVisible)
                Render();
        }

        public void Render()
        {
            var stats = player != null ? player.Stats : null;
            if (stats == null)
                return;

            if (levelText != null) levelText.text = stats.Level.ToString();
            if (attackText != null) attackText.text = stats.AttackPower.ToString("0.##");
            if (defenseText != null) defenseText.text = stats.Defense.ToString("0.##");
            if (moveSpeedText != null) moveSpeedText.text = stats.MoveSpeed.ToString("0.##");
            if (attackSpeedText != null) attackSpeedText.text = stats.AttackSpeed.ToString("0.##");
            if (jumpHeightText != null)
                jumpHeightText.text = (characterRoot != null ? characterRoot.JumpHeight : stats.JumpHeight).ToString("0.##");
        }

        private void SetVisible(bool visible)
        {
            if (panelGroup == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            panelGroup.alpha = visible ? 1f : 0f;
            panelGroup.interactable = visible;
            panelGroup.blocksRaycasts = visible;
        }

        private bool IsVisible => panelGroup == null ? gameObject.activeSelf : panelGroup.alpha > 0.01f;
    }
}

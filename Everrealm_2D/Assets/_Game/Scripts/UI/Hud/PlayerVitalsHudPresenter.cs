using LetterHunter.Characters;
using UnityEngine;

namespace LetterHunter.UI.Hud
{
    [DisallowMultipleComponent]
    public sealed class PlayerVitalsHudPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerClassController player;
        [SerializeField] private VitalsOrbView healthOrb;
        [SerializeField] private VitalsOrbView manaOrb;
        [SerializeField] private UnityEngine.UI.Text levelText;
        [SerializeField] private UnityEngine.UI.Slider experienceSlider;
        [SerializeField] private LetterHunter.Stats.PlayerLevelProgression progression;
        private int _displayedExperience = -1;

        private void Awake()
        {
            if (player == null)
                player = FindFirstObjectByType<PlayerClassController>();
            if (progression == null && player != null) progression = player.GetComponent<LetterHunter.Stats.PlayerLevelProgression>();
        }

        private void Update()
        {
            if (player == null || player.Stats == null)
                return;

            healthOrb?.Render(player.Stats.CurrentHealth, player.Stats.MaxHealth);
            manaOrb?.Render(player.Stats.CurrentMana, player.Stats.MaxMana);
            if (progression != null && _displayedExperience != progression.State.TotalExperience)
            {
                var state = progression.State;
                _displayedExperience = state.TotalExperience;
                if (levelText != null) levelText.text = state.IsMaxLevel
                    ? $"LEVEL {state.Level}  •  MAX"
                    : $"LEVEL {state.Level}  •  {state.CurrentExperience} / {state.RequiredExperience} XP";
                if (experienceSlider != null)
                {
                    experienceSlider.minValue = 0;
                    experienceSlider.maxValue = state.IsMaxLevel ? 1 : state.RequiredExperience;
                    experienceSlider.SetValueWithoutNotify(state.IsMaxLevel ? 1 : state.CurrentExperience);
                }
            }
        }
    }
}

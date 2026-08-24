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

        private void Awake()
        {
            if (player == null)
                player = FindFirstObjectByType<PlayerClassController>();
        }

        private void Update()
        {
            if (player == null || player.Stats == null)
                return;

            healthOrb?.Render(player.Stats.CurrentHealth, player.Stats.MaxHealth);
            manaOrb?.Render(player.Stats.CurrentMana, player.Stats.MaxMana);
        }
    }
}

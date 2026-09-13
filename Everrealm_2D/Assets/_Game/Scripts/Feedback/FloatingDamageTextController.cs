using LetterHunter.Combat;
using UnityEngine;

namespace LetterHunter.Feedback
{
    [DisallowMultipleComponent]
    public sealed class FloatingDamageTextController : MonoBehaviour
    {
        [Header("Presentation")]
        [SerializeField] private FloatingDamageTextType damageTextType = FloatingDamageTextType.Normal;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Vector2 localOffset = new(0f, 1.25f);
        [SerializeField] private bool roundDamage = true;
        [SerializeField] private bool showZeroDamageAsBlocked = true;

        [Header("Debug")]
        [SerializeField] private bool logSpawns;
        [SerializeField] private float testDamage = 12f;

        public FloatingDamageTextType DamageTextType
        {
            get => damageTextType;
            set => damageTextType = value;
        }

        public void ShowDamage(float amount, bool isCritical = false)
        {
            var type = amount <= 0f && showZeroDamageAsBlocked ? FloatingDamageTextType.Blocked : damageTextType;
            var text = FormatDamage(amount, type);
            ShowText(text, type, isCritical);
        }

        public void ShowDamage(DamageResult result)
        {
            var profile = result.ImpactProfile;
            var type = ResolveTextType(result, profile);
            var text = FormatDamage(result.FinalDamage, type);
            var textPosition = spawnPoint != null ? spawnPoint.position : transform.TransformPoint(localOffset);

            FloatingDamageTextManager.Show(new FloatingDamageTextRequest(textPosition, text, type, result.WasCritical, transform));

            if (profile != null)
            {
                var effectPosition = result.ImpactPosition.HasValue
                    ? (Vector3)result.ImpactPosition.Value
                    : transform.TransformPoint(profile.HitEffectOffset);
                ImpactEffectPool.Play(profile, effectPosition, result.AttackDirection);

                if (profile.LogFeedback)
                {
                    Debug.Log($"[Impact Feedback] {name}: {profile.ImpactId}, damage={result.FinalDamage:0.##}, text={type}, source={result.SourceSkillId}", this);
                }
            }

            if (logSpawns)
                Debug.Log($"[Floating Damage Text] {name}: '{text}' ({type}) at {textPosition}", this);
        }

        public void ShowText(string text, FloatingDamageTextType type, bool isCritical = false)
        {
            var position = spawnPoint != null ? spawnPoint.position : transform.TransformPoint(localOffset);
            FloatingDamageTextManager.Show(new FloatingDamageTextRequest(position, text, type, isCritical, transform));

            if (logSpawns)
                Debug.Log($"[Floating Damage Text] {name}: '{text}' ({type}) at {position}", this);
        }

        [ContextMenu("Test Damage Text")]
        private void TestDamageText()
        {
            ShowDamage(testDamage);
        }

        private string FormatDamage(float amount, FloatingDamageTextType type)
        {
            if (type == FloatingDamageTextType.Blocked) return "BLOCK";
            if (type == FloatingDamageTextType.Miss) return "MISS";

            var value = Mathf.Max(0f, amount);
            return roundDamage ? Mathf.RoundToInt(value).ToString() : value.ToString("0.##");
        }

        private FloatingDamageTextType ResolveTextType(DamageResult result, AttackImpactProfile profile)
        {
            if (result.FinalDamage <= 0f && showZeroDamageAsBlocked)
                return FloatingDamageTextType.Blocked;

            if (profile != null)
                return profile.GetTextType(result.WasCritical);

            return result.WasCritical ? FloatingDamageTextType.Critical : damageTextType;
        }
    }
}

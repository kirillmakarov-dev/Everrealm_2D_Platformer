using LetterHunter.Core;
using LetterHunter.Feedback;
using UnityEngine;

namespace LetterHunter.Combat
{
    [CreateAssetMenu(menuName = "Everrealm/Combat/Enemy Attack Definition")]
    public sealed class EnemyAttackDefinition : ScriptableObject
    {
        [SerializeField] private string attackId = "enemy_attack";
        [SerializeField] private string displayName = "Enemy Attack";
        [Min(0f), SerializeField] private float baseDamage = 8f;
        [Min(0f), SerializeField] private float damageMultiplier = 1f;
        [Min(1), SerializeField] private int damageLines = 1;
        [Min(0.05f), SerializeField] private float cooldown = 1.25f;
        [SerializeField] private DamageTag tags = DamageTag.Melee;
        [SerializeField] private AttackImpactProfile impactProfile;

        public string AttackId => string.IsNullOrWhiteSpace(attackId) ? name : attackId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public float BaseDamage => Mathf.Max(0f, baseDamage);
        public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);
        public int DamageLines => Mathf.Max(1, damageLines);
        public float Cooldown => Mathf.Max(0.05f, cooldown);
        public DamageTag Tags => tags;
        public AttackImpactProfile ImpactProfile => impactProfile;
    }
}

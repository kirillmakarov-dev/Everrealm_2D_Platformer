using LetterHunter.Combat;
using LetterHunter.Core;
using LetterHunter.Characters;
using LetterHunter.Feedback;
using LetterHunter.Loot;
using LetterHunter.Stats;
using UnityEngine;

namespace LetterHunter.Debugging
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(FloatingDamageTextController))]
    public sealed class DummyEnemy2D : MonoBehaviour, ICombatActor, IExperienceReward
    {
        [SerializeField] private CombatStatsData stats = default;
        [SerializeField] private CharacterClassType classType = CharacterClassType.Warrior;
        [SerializeField] private FloatingDamageTextController damageTextController;
        [Header("Death Feedback")]
        [Min(0f), SerializeField] private float deathUpwardVelocity = 5.5f;
        // Kept for backwards-compatible prefab/scene serialization. Death movement
        // is intentionally vertical now, so this legacy value is never applied.
#pragma warning disable CS0414
        [Min(0f), SerializeField] private float deathHorizontalVelocity = 1.25f;
#pragma warning restore CS0414
        [Min(0f), SerializeField] private float deathGravityScale = 3.5f;
        [Min(0.05f), SerializeField] private float destroyAfterDeath = 1.6f;
        [SerializeField] private bool disableCollidersOnDeath = true;
        [Header("Hit Reaction")]
        [SerializeField] private CharacterHitReaction2D hitReaction;
        [Min(0f), SerializeField] private float hitKnockbackHorizontal = 3.75f;
        [Min(0f), SerializeField] private float hitKnockbackVertical = 1.25f;
        [Min(0f), SerializeField] private float hitMovementLockDuration = 0.16f;
        [SerializeField] private MonsterLoot monsterLoot;
        [Header("Progression")]
        [Min(0), SerializeField] private int experienceReward = 25;
        public int ExperienceReward => Mathf.Max(0, experienceReward);

        public Transform Transform => transform;
        public CombatStats Stats { get; private set; }
        public CharacterClassType ClassType => classType;
        public bool IsAlive => !_defeated && Stats != null && Stats.CurrentHealth > 0f;

        private Rigidbody2D _body;
        private bool _defeated;

        private void Awake()
        {
            Stats = new CombatStats(stats.maxHealth > 0f ? stats : CombatStatsData.Default);
            _body = GetComponent<Rigidbody2D>();
            _body.freezeRotation = true;

            if (damageTextController == null)
                damageTextController = GetComponent<FloatingDamageTextController>();
            if (damageTextController == null)
                damageTextController = gameObject.AddComponent<FloatingDamageTextController>();
            if (hitReaction == null)
                hitReaction = GetComponent<CharacterHitReaction2D>();
            if (hitReaction == null)
                hitReaction = gameObject.AddComponent<CharacterHitReaction2D>();
            if (monsterLoot == null)
                monsterLoot = GetComponent<MonsterLoot>();
        }

        public void ReceiveDamage(DamageResult result)
        {
            if (_defeated)
                return;

            var amount = result.FinalDamage;
            Stats.TakeDamage(amount);
            damageTextController.ShowDamage(result);
            Debug.Log($"{name} received {amount:0.##} damage. HP: {Stats.CurrentHealth:0.##}/{Stats.MaxHealth:0.##}", this);
            if (!IsAlive)
            {
                Debug.Log($"{name} has been defeated.", this);
                BeginDeath();
                return;
            }

            hitReaction?.ApplyKnockback(result.AttackDirection, hitKnockbackHorizontal,
                hitKnockbackVertical, hitMovementLockDuration);
        }

        private void BeginDeath()
        {
            _defeated = true;
            monsterLoot?.DropLoot();

            foreach (var behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour != this && behaviour is not FloatingDamageTextController)
                    behaviour.enabled = false;
            }

            if (disableCollidersOnDeath)
            {
                foreach (var collider in GetComponentsInChildren<Collider2D>())
                    collider.enabled = false;
            }

            _body.gravityScale = deathGravityScale;
            // Death is a vertical fall onto the current surface. Do not carry
            // patrol or hit-reaction velocity into the corpse, otherwise it slides
            // across the platform while the death animation is playing.
            _body.linearVelocity = new Vector2(0f, deathUpwardVelocity);

            Destroy(gameObject, destroyAfterDeath);
        }
    }
}

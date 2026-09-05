using LetterHunter.Debugging;
using UnityEngine;

namespace LetterHunter.Characters
{
    /// <summary>Presentation only. Attach to the visual child so death cleanup can finish playing.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private DummyEnemy2D enemy;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private EnemyAttackController2D attacks;

        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int Hurt = Animator.StringToHash("Hurt");
        private static readonly int Dead = Animator.StringToHash("Dead");
        private double _health;
        private bool _initialized;
        private bool _dead;

        private void OnEnable()
        {
            if (animator == null || enemy == null || body == null || attacks == null)
            {
                Debug.LogError("EnemySpriteAnimator needs Animator, Enemy, Body and Attacks references.", this);
                enabled = false;
                return;
            }
            _initialized = false;
            _dead = false;
            animator.SetBool(Dead, false);
            animator.ResetTrigger(Attack);
            animator.ResetTrigger(Hurt);
            attacks.AttackPerformed += OnAttack;
        }

        private void OnDisable()
        {
            if (attacks != null) attacks.AttackPerformed -= OnAttack;
        }

        private void OnAttack()
        {
            if (!_dead && enemy.IsAlive) animator.SetTrigger(Attack);
        }

        private void LateUpdate()
        {
            if (enemy.Stats == null || _dead) return;
            var health = enemy.Stats.CurrentHealth;
            if (!enemy.IsAlive)
            {
                _dead = true;
                animator.ResetTrigger(Attack);
                animator.ResetTrigger(Hurt);
                animator.SetBool(Dead, true);
                animator.SetFloat(Speed, 0f);
                return;
            }
            if (_initialized && health < _health) animator.SetTrigger(Hurt);
            _health = health;
            _initialized = true;
            animator.SetFloat(Speed, Mathf.Abs(body.linearVelocity.x));
        }
    }
}

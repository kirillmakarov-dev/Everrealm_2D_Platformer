using UnityEngine;

namespace LetterHunter.Characters
{
    public sealed class CharacterAnimationController : MonoBehaviour, IAnimationController
    {
        [SerializeField] private Animator animator;
        private static readonly int StateHash = Animator.StringToHash("CharacterState");
        private static readonly int SpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int DeadHash = Animator.StringToHash("IsDead");

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            if (animator == null)
                Debug.LogWarning("CharacterAnimationController could not find an Animator on the character or its visual child.", this);
        }

        private void OnValidate()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
        }

        public void PlayIdle()
        {
            SetState(CharacterStateId.Idle);
            SetMoveSpeed(0f);
        }

        public void PlayRun(float normalizedSpeed)
        {
            // Walk and Run are children of the locomotion Blend Tree.
            SetState(CharacterStateId.Idle);
            SetMoveSpeed(normalizedSpeed);
        }
        public void SetMoveSpeed(float normalizedSpeed)
        {
            if (animator != null)
                animator.SetFloat(SpeedHash, Mathf.Clamp01(normalizedSpeed));
        }
        public void PlayJump() => SetState(CharacterStateId.Jump);

        public void PlayFall() => SetState(CharacterStateId.Fall);

        public void PlayAttack() => SetState(CharacterStateId.Attack);
        public void PlayDead()
        {
            if (animator == null) return;
            animator.SetBool(DeadHash, true);
            SetState(CharacterStateId.Dead);
        }

        private void SetState(CharacterStateId state)
        {
            if (animator == null) return;
            animator.SetInteger(StateHash, (int)state);
            animator.SetBool(DeadHash, state == CharacterStateId.Dead);
        }

    }
}

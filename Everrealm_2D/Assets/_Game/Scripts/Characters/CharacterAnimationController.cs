using UnityEngine;

namespace LetterHunter.Characters
{
    public sealed class CharacterAnimationController : MonoBehaviour, IAnimationController
    {
        [SerializeField] private Animator animator;
        private static readonly int StateHash = Animator.StringToHash("CharacterState");
        private static readonly int SpeedHash = Animator.StringToHash("MoveSpeed");

        public void PlayIdle() => SetState(CharacterStateId.Idle);
        public void PlayRun(float normalizedSpeed) { SetState(CharacterStateId.Run); if (animator != null) animator.SetFloat(SpeedHash, normalizedSpeed); }
        public void PlayJump() => SetState(CharacterStateId.Jump);
        public void PlayFall() => SetState(CharacterStateId.Fall);
        public void PlayAttack() => SetState(CharacterStateId.Attack);
        private void SetState(CharacterStateId state) { if (animator != null) animator.SetInteger(StateHash, (int)state); }
    }
}

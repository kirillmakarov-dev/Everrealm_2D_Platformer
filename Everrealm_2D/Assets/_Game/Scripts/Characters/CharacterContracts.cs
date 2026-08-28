using System;
using UnityEngine;

namespace LetterHunter.Characters
{
    public interface ICharacterMotor2D
    {
        Vector2 Velocity { get; }
        void SetHorizontalVelocity(float velocity);
        void SetVerticalVelocity(float velocity);
        void ConfigureGravity(float gravityScale);
    }

    public interface IMovementController
    {
        Vector2 MoveInput { get; }
        void SetMoveInput(Vector2 input);
        void Tick(float deltaTime);
    }

    public interface IGroundDetector
    {
        bool IsGrounded { get; }
        event Action Landed;
        event Action LeftGround;
    }

    public interface IJumpController { bool TryJump(); }

    public interface IFacingController
    {
        Vector2 Direction { get; }
        void UpdateFacing(Vector2 movementDirection);
    }

    public interface ICharacterFacingView { void ApplyFacing(Vector2 direction); }

    public interface IAnimationController
    {
        void PlayIdle();
        void PlayRun(float normalizedSpeed);
        void PlayJump();
        void PlayFall();
        void PlayAttack();
        void PlayDead();
    }
}

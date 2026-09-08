using System;
using UnityEngine;

namespace LetterHunter.Characters
{
    public sealed class CharacterMovementController : IMovementController
    {
        private readonly ICharacterMotor2D _motor;
        private readonly IGroundDetector _ground;
        private readonly CharacterMovementConfig _config;
        private readonly CharacterRuntime _runtime;
        private readonly Func<float> _moveSpeed;

        public CharacterMovementController(ICharacterMotor2D motor, IGroundDetector ground,
            CharacterMovementConfig config, CharacterRuntime runtime, Func<float> moveSpeed = null)
        { _motor = motor; _ground = ground; _config = config; _runtime = runtime; _moveSpeed = moveSpeed; }

        public Vector2 MoveInput { get; private set; }
        public void SetMoveInput(Vector2 input) => MoveInput = Vector2.ClampMagnitude(input, 1f);

        public void Tick(float deltaTime)
        {
            var target = MoveInput.x * Mathf.Max(0f, _moveSpeed?.Invoke() ?? _config.MoveSpeed);
            var accelerating = Mathf.Abs(target) > Mathf.Abs(_motor.Velocity.x);
            var rate = accelerating ? _config.Acceleration : _config.Deceleration;
            if (!_ground.IsGrounded) rate *= _config.AirControl;
            _motor.SetHorizontalVelocity(Mathf.MoveTowards(_motor.Velocity.x, target, rate * deltaTime));
        }
    }
}

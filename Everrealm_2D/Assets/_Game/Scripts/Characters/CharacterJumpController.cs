using System;

namespace LetterHunter.Characters
{
    public sealed class CharacterJumpController : IJumpController
    {
        private readonly ICharacterMotor2D _motor;
        private readonly IGroundDetector _ground;
        private readonly CharacterMovementConfig _config;
        private readonly Func<float> _jumpVelocity;

        public CharacterJumpController(ICharacterMotor2D motor, IGroundDetector ground, CharacterMovementConfig config,
            Func<float> jumpVelocity = null)
        { _motor = motor; _ground = ground; _config = config; _jumpVelocity = jumpVelocity; }

        public bool TryJump()
        {
            if (!_ground.IsGrounded) return false;
            _motor.SetVerticalVelocity((float)(_jumpVelocity?.Invoke() ?? _config.JumpForce));
            return true;
        }
    }
}

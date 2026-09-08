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

        private float _coyoteRemaining;
        private float _bufferRemaining;
        private bool _requested;
        private bool _consumed;
        private bool _leftGround;

        public void RequestJump()
        {
            _requested = true;
            _bufferRemaining = _config.JumpBufferTime;
        }

        public bool Tick(float deltaTime)
        {
            if (!_ground.IsGrounded) _leftGround = true;
            if (_ground.IsGrounded && _motor.Velocity.y <= .01f)
            {
                if (_leftGround) { _consumed = false; _leftGround = false; }
                if (!_consumed) _coyoteRemaining = _config.CoyoteTime;
            }
            else _coyoteRemaining = Math.Max(0f, _coyoteRemaining - deltaTime);

            if (_requested && TryJump()) { _requested = false; return true; }
            _bufferRemaining -= deltaTime;
            if (_bufferRemaining <= 0f) _requested = false;
            return false;
        }

        public bool TryJump()
        {
            if (_consumed || (!_ground.IsGrounded && _coyoteRemaining <= 0f)) return false;
            _consumed = true;
            _coyoteRemaining = 0f;
            _motor.SetVerticalVelocity((float)(_jumpVelocity?.Invoke() ?? _config.JumpForce));
            return true;
        }
    }
}

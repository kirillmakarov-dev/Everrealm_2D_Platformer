using UnityEngine;

namespace LetterHunter.Characters
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CharacterMotor2D : MonoBehaviour, ICharacterMotor2D
    {
        private Rigidbody2D _body;
        public Vector2 Velocity => Body.linearVelocity;
        private Rigidbody2D Body => _body != null ? _body : _body = GetComponent<Rigidbody2D>();

        private void Awake() => _body = GetComponent<Rigidbody2D>();

        public void SetHorizontalVelocity(float velocity)
        {
            var current = Body.linearVelocity;
            Body.linearVelocity = new Vector2(velocity, current.y);
        }

        public void SetVerticalVelocity(float velocity)
        {
            var current = Body.linearVelocity;
            Body.linearVelocity = new Vector2(current.x, velocity);
        }

        public void ConfigureGravity(float gravityScale) => Body.gravityScale = gravityScale;
    }
}

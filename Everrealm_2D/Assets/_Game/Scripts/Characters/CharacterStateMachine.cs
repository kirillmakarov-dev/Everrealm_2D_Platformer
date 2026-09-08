using LetterHunter.Core.StateMachine;
using System;
using System.Collections.Generic;

namespace LetterHunter.Characters
{
    public sealed class CharacterStateMachine
    {
        private readonly CharacterRuntime _runtime;
        private readonly StateMachine<CharacterStateId, CharacterRuntime> _machine = new();
        private readonly List<Func<CharacterRuntime, CharacterStateId?>> _transitionRules = new();
        private float _minimumFallDistance;
        private float _attackRemaining;
        private bool _jumpStarted;

        public void NotifyJump() => _jumpStarted = true;

        public CharacterStateMachine(CharacterRuntime runtime, float minimumFallDistance = .5f)
        {
            _runtime = runtime;
            _minimumFallDistance = Math.Max(0f, minimumFallDistance);
            foreach (CharacterStateId state in System.Enum.GetValues(typeof(CharacterStateId)))
                _machine.Register(state, new CharacterState());
            _machine.StateChanged += (_, next) => _runtime.CurrentState = next;
            _machine.ChangeState(CharacterStateId.Idle, _runtime);
            _transitionRules.Add(EvaluateDefaultTransition);
        }

        public void SetMinimumFallDistance(float distance)
        {
            _minimumFallDistance = Math.Max(0f, distance);
        }

        public event System.Action<CharacterStateId, CharacterStateId> StateChanged
        { add => _machine.StateChanged += value; remove => _machine.StateChanged -= value; }

        public void BeginAttack(float duration) => _attackRemaining = System.Math.Max(.01f, duration);

        public void RegisterState(CharacterStateId id, IState<CharacterRuntime> state) => _machine.Register(id, state);

        public void AddTransitionRule(Func<CharacterRuntime, CharacterStateId?> rule, bool highPriority = false)
        {
            if (rule == null) return;
            if (highPriority) _transitionRules.Insert(0, rule); else _transitionRules.Add(rule);
        }

        public void Tick(float deltaTime)
        {
            if (_runtime.Grounded) _jumpStarted = false;
            foreach (var rule in _transitionRules)
            {
                var next = rule(_runtime);
                if (!next.HasValue) continue;
                _machine.ChangeState(next.Value, _runtime);
                break;
            }
            if (_attackRemaining > 0f) _attackRemaining -= deltaTime;

            _machine.Tick(_runtime, deltaTime);
        }

        private CharacterStateId? EvaluateDefaultTransition(CharacterRuntime runtime)
        {
            if (runtime.IsDead) return CharacterStateId.Dead;
            if (_attackRemaining > 0f) return CharacterStateId.Attack;
            if (!runtime.Grounded)
                return EvaluateAirborneState(runtime);
            return Math.Abs(runtime.CurrentVelocity.x) > .05f ? CharacterStateId.Run : CharacterStateId.Idle;
        }

        private CharacterStateId EvaluateAirborneState(CharacterRuntime runtime)
        {
            if (runtime.CurrentVelocity.y < -.01f &&
                runtime.AirborneDropDistance >= _minimumFallDistance)
                return CharacterStateId.Fall;
            // Retain the takeoff pose through the apex, without replaying Jump.
            // Walking off an edge must never manufacture a jump.
            if (_jumpStarted) return CharacterStateId.Jump;
            return Math.Abs(runtime.CurrentVelocity.x) > .05f
                ? CharacterStateId.Run : CharacterStateId.Idle;
        }

        private sealed class CharacterState : IState<CharacterRuntime>
        {
            public void Enter(CharacterRuntime context) { }
            public void Exit(CharacterRuntime context) { }
            public void Tick(CharacterRuntime context, float deltaTime) { }
        }
    }
}

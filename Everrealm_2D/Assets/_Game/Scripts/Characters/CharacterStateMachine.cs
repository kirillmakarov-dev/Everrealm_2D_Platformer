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
        private float _attackRemaining;

        public CharacterStateMachine(CharacterRuntime runtime)
        {
            _runtime = runtime;
            foreach (CharacterStateId state in System.Enum.GetValues(typeof(CharacterStateId)))
                _machine.Register(state, new CharacterState());
            _machine.StateChanged += (_, next) => _runtime.CurrentState = next;
            _machine.ChangeState(CharacterStateId.Idle, _runtime);
            _transitionRules.Add(EvaluateDefaultTransition);
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
            if (!runtime.Grounded) return runtime.CurrentVelocity.y > .01f ? CharacterStateId.Jump : CharacterStateId.Fall;
            return Math.Abs(runtime.CurrentVelocity.x) > .05f ? CharacterStateId.Run : CharacterStateId.Idle;
        }

        private sealed class CharacterState : IState<CharacterRuntime>
        {
            public void Enter(CharacterRuntime context) { }
            public void Exit(CharacterRuntime context) { }
            public void Tick(CharacterRuntime context, float deltaTime) { }
        }
    }
}

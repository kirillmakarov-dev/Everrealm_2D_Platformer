using System;
using System.Collections.Generic;

namespace LetterHunter.Core.StateMachine
{
    public interface IState<in TContext>
    {
        void Enter(TContext context);
        void Exit(TContext context);
        void Tick(TContext context, float deltaTime);
    }

    public sealed class StateMachine<TState, TContext> where TState : struct, Enum
    {
        private readonly Dictionary<TState, IState<TContext>> _states = new();

        public TState CurrentId { get; private set; }
        public IState<TContext> Current { get; private set; }
        public event Action<TState, TState> StateChanged;

        public void Register(TState id, IState<TContext> state) => _states[id] = state;

        public bool ChangeState(TState next, TContext context)
        {
            if (Current != null && EqualityComparer<TState>.Default.Equals(CurrentId, next)) return false;
            if (!_states.TryGetValue(next, out var state)) return false;
            var previous = CurrentId;
            Current?.Exit(context);
            CurrentId = next;
            Current = state;
            Current.Enter(context);
            StateChanged?.Invoke(previous, next);
            return true;
        }

        public void Tick(TContext context, float deltaTime) => Current?.Tick(context, deltaTime);
    }
}

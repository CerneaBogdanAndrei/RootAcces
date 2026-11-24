using System;
using UnityEngine;

namespace RootAccess.Core
{
    public interface IState
    {
        void Enter();
        void Tick(float deltaTime);
        void FixedTick(float deltaTime);
        void Exit();
    }

    /// <summary>Convenience base class giving states typed access to their owner.</summary>
    public abstract class StateBase<TOwner> : IState
    {
        protected readonly TOwner Owner;

        protected StateBase(TOwner owner) => Owner = owner;

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void FixedTick(float deltaTime) { }
        public virtual void Exit() { }

        public override string ToString() => GetType().Name;
    }

    /// <summary>Plain C# finite state machine shared by the player controller and every AI agent.</summary>
    public sealed class StateMachine
    {
        public IState Current { get; private set; }
        public IState Previous { get; private set; }
        public float TimeInState { get; private set; }

        public event Action<IState, IState> StateChanged;

        public void ChangeState(IState next)
        {
            if (next == null)
            {
                Debug.LogError("[StateMachine] Refusing to change to a null state.");
                return;
            }

            if (ReferenceEquals(next, Current)) return;

            Current?.Exit();
            Previous = Current;
            Current = next;
            TimeInState = 0f;
            Current.Enter();
            StateChanged?.Invoke(Previous, Current);
        }

        public bool IsIn(IState state) => ReferenceEquals(Current, state);

        public void Tick(float deltaTime)
        {
            TimeInState += deltaTime;
            Current?.Tick(deltaTime);
        }

        public void FixedTick(float deltaTime) => Current?.FixedTick(deltaTime);
    }
}

using RootAccess.Core;
using UnityEngine;

namespace RootAccess.AI
{
    public abstract class GuardStateBase : StateBase<Guard>
    {
        protected GuardStateBase(Guard owner) : base(owner) { }
        public abstract GuardStateId Id { get; }
    }

    /// <summary>Walks between patrol points, pausing at each. Any awareness makes the guard suspicious.</summary>
    public sealed class GuardPatrolState : GuardStateBase
    {
        private int _index;
        private float _waitUntil;

        public GuardPatrolState(Guard owner) : base(owner) { }
        public override GuardStateId Id => GuardStateId.Patrol;

        public override void Tick(float deltaTime)
        {
            if (Owner.Awareness > 0.05f) Owner.ChangeState(GuardStateId.Suspicious);
        }

        public override void FixedTick(float deltaTime)
        {
            Transform[] points = Owner.PatrolPoints;
            if (points.Length == 0 || Time.time < _waitUntil)
            {
                Owner.Stop();
                return;
            }

            Transform target = points[_index % points.Length];
            if (target == null || !Owner.MoveTowardsX(target.position.x, Owner.Config.PatrolSpeed)) return;
            _index = (_index + 1) % points.Length;
            _waitUntil = Time.time + Owner.Config.PatrolWaitSeconds;
        }
    }

    /// <summary>Stops and stares at the last seen position while awareness fills (full → chase, empty → patrol).</summary>
    public sealed class GuardSuspiciousState : GuardStateBase
    {
        public GuardSuspiciousState(Guard owner) : base(owner) { }
        public override GuardStateId Id => GuardStateId.Suspicious;

        public override void Enter() => Owner.Stop();

        public override void Tick(float deltaTime)
        {
            float dx = Owner.LastKnownPlayerPosition.x - Owner.Position.x;
            if (Mathf.Abs(dx) > 0.1f) Owner.Face(dx > 0f ? 1 : -1);

            if (Owner.Awareness >= 1f)
            {
                EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Spotted, Owner.transform.position));
                Owner.ChangeState(GuardStateId.Chase);
            }
            else if (Owner.Awareness <= 0f)
            {
                Owner.ChangeState(GuardStateId.Return);
            }
        }

        public override void FixedTick(float deltaTime) => Owner.Stop();
    }

    /// <summary>Runs at the player, attacking in range. Losing sight for a while switches to searching.</summary>
    public sealed class GuardChaseState : GuardStateBase
    {
        private float _lostSightTimer;

        public GuardChaseState(Guard owner) : base(owner) { }
        public override GuardStateId Id => GuardStateId.Chase;

        public override void Enter()
        {
            _lostSightTimer = 0f;
            Owner.SetAwareness(1f);
            EventBus<TraceNoise>.Publish(new TraceNoise(15f, TraceSource.Guard, Owner.LastKnownPlayerPosition));
        }

        public override void Tick(float deltaTime)
        {
            _lostSightTimer = Owner.CanSeePlayer ? 0f : _lostSightTimer + deltaTime;
            if (_lostSightTimer >= Owner.Config.LoseSightSeconds) Owner.ChangeState(GuardStateId.Search);
            else Owner.TryAttack();
        }

        public override void FixedTick(float deltaTime) =>
            Owner.MoveTowardsX(Owner.LastKnownPlayerPosition.x, Owner.Config.ChaseSpeed, Owner.Config.AttackRange * 0.7f);
    }

    /// <summary>Goes to the last known position and looks both ways before giving up.</summary>
    public sealed class GuardSearchState : GuardStateBase
    {
        private float _nextTurn;

        public GuardSearchState(Guard owner) : base(owner) { }
        public override GuardStateId Id => GuardStateId.Search;

        public override void Enter()
        {
            _nextTurn = Time.time + 1f;
            Owner.SetAwareness(0.6f);
        }

        public override void Tick(float deltaTime)
        {
            if (Owner.CanSeePlayer && Owner.Awareness >= 0.85f)
            {
                Owner.ChangeState(GuardStateId.Chase);
                return;
            }

            if (Owner.TimeInState >= Owner.Config.SearchSeconds)
            {
                Owner.SetAwareness(0f);
                Owner.ChangeState(GuardStateId.Return);
                return;
            }

            if (Time.time < _nextTurn) return;
            _nextTurn = Time.time + 1.1f;
            Owner.Face(-Owner.Facing);
        }

        public override void FixedTick(float deltaTime) =>
            Owner.MoveTowardsX(Owner.LastKnownPlayerPosition.x, Owner.Config.PatrolSpeed * 1.4f, 0.4f);
    }

    /// <summary>Walks back to the patrol route and resumes patrolling.</summary>
    public sealed class GuardReturnState : GuardStateBase
    {
        public GuardReturnState(Guard owner) : base(owner) { }
        public override GuardStateId Id => GuardStateId.Return;

        public override void Tick(float deltaTime)
        {
            if (Owner.Awareness > 0.3f) Owner.ChangeState(GuardStateId.Suspicious);
        }

        public override void FixedTick(float deltaTime)
        {
            Transform[] points = Owner.PatrolPoints;
            float targetX = points.Length > 0 && points[0] != null ? points[0].position.x : Owner.PatrolStart.x;
            if (Owner.MoveTowardsX(targetX, Owner.Config.PatrolSpeed)) Owner.ChangeState(GuardStateId.Patrol);
        }
    }
}

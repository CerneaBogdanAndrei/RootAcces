using UnityEngine;

namespace RootAccess.AI
{
    /// <summary>Per-archetype guard tuning (base, heavy, sniper variants each reference their own asset).</summary>
    [CreateAssetMenu(menuName = "Root Access/AI/Guard Config", fileName = "GuardConfig")]
    public sealed class GuardConfig : ScriptableObject
    {
        [SerializeField] private float patrolSpeed = 1.8f;
        [SerializeField] private float chaseSpeed = 5f;
        [SerializeField] private float patrolWaitSeconds = 1.2f;
        [SerializeField] private float viewDistance = 7f;
        [SerializeField] private float viewAngle = 70f;
        [SerializeField] private float awarenessGainPerSecond = 1.4f;
        [SerializeField] private float awarenessDecayPerSecond = 0.5f;
        [SerializeField] private float loseSightSeconds = 1.5f;
        [SerializeField] private float searchSeconds = 4f;
        [SerializeField] private float attackRange = 1.1f;
        [SerializeField] private float attackCooldown = 1.1f;
        [SerializeField, Min(1)] private int attackDamage = 1;
        [SerializeField] private float tracePerSecondWhileSeen = 18f;

        public float PatrolSpeed => patrolSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float PatrolWaitSeconds => patrolWaitSeconds;
        public float ViewDistance => viewDistance;
        public float ViewAngle => viewAngle;
        public float AwarenessGainPerSecond => awarenessGainPerSecond;
        public float AwarenessDecayPerSecond => awarenessDecayPerSecond;
        public float LoseSightSeconds => loseSightSeconds;
        public float SearchSeconds => searchSeconds;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public int AttackDamage => attackDamage;
        public float TracePerSecondWhileSeen => tracePerSecondWhileSeen;

        public void Configure(float patrol, float chase, float distance, float angle, float gain, int damage, float trace)
        {
            patrolSpeed = patrol;
            chaseSpeed = chase;
            viewDistance = distance;
            viewAngle = angle;
            awarenessGainPerSecond = gain;
            attackDamage = Mathf.Max(1, damage);
            tracePerSecondWhileSeen = trace;
        }
    }
}

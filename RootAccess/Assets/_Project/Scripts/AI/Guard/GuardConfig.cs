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
        [SerializeField, Tooltip("Multiplies the radius of every PlayerNoise this guard can hear.")]
        private float hearingSensitivity = 1f;
        [SerializeField, Tooltip("Seconds spent looking around after reaching a noise before returning to patrol.")]
        private float investigateSeconds = 3f;
        [SerializeField, Tooltip("Guards closer than this to a raised alarm converge on it.")]
        private float alarmResponseRadius = 22f;
        [SerializeField, Tooltip("Ranged guards (sniper) shoot projectiles instead of hitting in melee.")]
        private bool ranged;

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
        public float HearingSensitivity => hearingSensitivity;
        public float InvestigateSeconds => investigateSeconds;
        public float AlarmResponseRadius => alarmResponseRadius;
        public bool Ranged => ranged;

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

        /// <summary>Hearing and alarm response; ranged guards also get their own attack range and cadence.</summary>
        public void ConfigureSenses(float hearing, float alarmRadius, bool isRanged = false, float range = 1.1f, float cooldown = 1.1f)
        {
            hearingSensitivity = Mathf.Max(0f, hearing);
            alarmResponseRadius = Mathf.Max(0f, alarmRadius);
            ranged = isRanged;
            attackRange = Mathf.Max(0.5f, range);
            attackCooldown = Mathf.Max(0.2f, cooldown);
        }
    }
}
